using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Progress;
using Drift.Domain.Scan;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan.Scanners;

public class PingNetworkScanner( IPingTool pingTool ) : INetworkScanner {
  public event EventHandler<NetworkScanResult>? ResultUpdated;

  public Task<NetworkScanResult> ScanAsync(
    NetworkScanOptions request,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  ) {
    return ScanAsyncOld( request, logger, null, cancellationToken );
  }

  public async Task<NetworkScanResult> ScanAsyncOld(
    NetworkScanOptions request,
    ILogger? logger = null,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default
  ) {
    var startedAt = DateTime.Now;
    logger?.LogDebug( "Starting network scan at {StartedAt}", startedAt.ToString( CultureInfo.InvariantCulture ) );

    EventHandler<SubnetScanResult> eventHandler = ( ( _, result ) => ResultUpdated?.Invoke( null,
      new NetworkScanResult { Metadata = null, Status = ScanResultStatus.InProgress, Subnets = [result], Progress = result.Progress } ) );

    var localSubnetScanner = new LocalSubnetScanner( pingTool );
    localSubnetScanner.ResultUpdated += eventHandler;

    try {
      var subnetScanners = request.Cidrs.Select( c =>
        localSubnetScanner.ScanAsync(
          new SubnetScanOptions { Cidr = c }, logger, onProgress, cancellationToken )
      ).ToList();

      await Task.WhenAll( subnetScanners );

      var finishedAt = DateTime.Now;
      var elapsed =
        finishedAt - startedAt; // TODO .Humanize( 2, CultureInfo.InvariantCulture, minUnit: TimeUnit.Second )
      logger?.LogDebug( "Finished network scan at {StartedAt} in {Elapsed}",
        finishedAt.ToString( CultureInfo.InvariantCulture ),
        elapsed
      );

      return new NetworkScanResult {
        Metadata = new Metadata { StartedAt = startedAt, EndedAt = DateTime.Now },
        Status = ScanResultStatus.Success,
        Progress = Percentage.Hundred,
        Subnets = subnetScanners.Select( t => t.Result )
      };
    }
    finally {
      localSubnetScanner.ResultUpdated -= eventHandler;
    }
  }


  private async Task PingScanAsync( ConcurrentBag<(CidrBlock cidr, string Ip, bool Success, string? Hostname)> results,
    CidrBlock cidr,
    ILogger? logger,
    uint maxPingsPerSecond,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default
  ) {
    var ipRange = IPNetwork2
      .Parse( cidr.ToString() )
      .ListIPAddress( FilterEnum.Usable )
      .Select( ip => ip.ToString() )
      .ToList();

    var total = ipRange.Count;
    var completed = 0u;

    if ( total == 0 ) {
      logger?.LogWarning( "Skipping ping scan for CIDR block {Cidr} as it has no usable IP addresses", cidr );
      return;
    }

    logger?.LogDebug( "Starting ping scan for CIDR block {Cidr} ({Total} addresses)", cidr, total );

    using var throttler = new SemaphoreSlim( (int) maxPingsPerSecond );

    var pingTasks = ipRange.Select( async ip => {
      await throttler.WaitAsync( cancellationToken );

      try {
        var success = ( await pingTool.RunAsync( $"-c 1 -W 1 {ip}" ) ).ExitCode == 0;
        string? hostname = "";
        if ( success ) {
          logger?.LogDebug( "Got reply from {Ip}", ip );
          hostname = await GetHostNameAsync( ip, 15 );
          //Console.WriteLine( hostname );
        }

        results.Add( ( cidr, ip, success, hostname ) );

        Interlocked.Increment( ref completed );

        if ( success ) {
          ResultUpdated?.Invoke( null,
            new NetworkScanResult {
              Metadata = null,
              Status = ScanResultStatus.InProgress,
              Subnets =
                ToDiscoveredDevices( results, ArpHelper.GetSystemCachedIpToMacMap(), finished: false ),
              Progress = new((byte) Math.Ceiling( ( (double) completed / total ) * 100 ))
            }
          );
        }

        onProgress?.Invoke( new ProgressReport {
          Tasks = [
            new TaskProgress {
              TaskName = "Ping Scan", CompletionPct = (uint) Math.Ceiling( ( (double) completed / total ) * 100 )
            }
          ]
        } );
      }
      finally {
        _ = Task.Delay( (int) ( 1000u / maxPingsPerSecond ), cancellationToken )
          .ContinueWith( _ => {
            try {
              throttler.Release();
            }
            // Justification: enable when throttler is fixed
#pragma warning disable CS0168 // Variable is declared but never used
            catch ( Exception ex ) {
#pragma warning restore CS0168 // Variable is declared but never used
              //Console.WriteLine(ex);
              //TODO throttler not working!!!
            }
          }, cancellationToken );
      }
    } ).ToList();

    await Task.WhenAll( pingTasks );

    logger?.LogDebug( "Finished ping scan for CIDR block {Cidr}", cidr );
  }

  private static IEnumerable<SubnetScanResult> ToDiscoveredDevices(
    ConcurrentBag<(CidrBlock cidr, string Ip, bool Success, string? Hostname)> pingReplies,
    Dictionary<string, string> ipToMac, bool finished ) {
    return pingReplies
      .Where( r => r.Success )
      .GroupBy( r => r.cidr )
      .Select( group => new SubnetScanResult {
        Metadata = null,
        Status = finished ? ScanResultStatus.Success : ScanResultStatus.InProgress,
        CidrBlock = group.Key,
        DiscoveredDevices = group.Select( pingReply =>
          new DiscoveredDevice { Addresses = CreateAddresses( pingReply ) }
        )
      } );

    List<IDeviceAddress> CreateAddresses( (CidrBlock cidr, string Ip, bool Success, string? Hostname) pingReply ) {
      var list = new List<IDeviceAddress> { new IpV4Address( pingReply.Ip ), };

      if ( !string.IsNullOrWhiteSpace( pingReply.Hostname ) ) {
        list.Add( new HostnameAddress( pingReply.Hostname ) );
      }

      if ( ipToMac.TryGetValue( pingReply.Ip, out var macStr ) ) {
        list.Add( new MacAddress( macStr ) );
      }

      return list;
    }
  }

  private static async Task<string?> GetHostNameAsync( string ip, int timeoutMs = 1000 ) {
    var task = Dns.GetHostEntryAsync( ip );
    if ( await Task.WhenAny( task, Task.Delay( timeoutMs ) ) == task ) {
      try {
        return task.Result.HostName;
      }
      catch {
        // Reverse DNS failed
        return null;
      }
    }

    // Timed out
    return null;
  }

  //TODO read from /proc/net/arp instead
  private static class ArpHelper {
    public static Dictionary<string, string> GetSystemCachedIpToMacMap() {
      var map = new Dictionary<string, string>();

      var startInfo = new ProcessStartInfo {
        FileName = "arp",
        Arguments = "-en",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      using var proc = Process.Start( startInfo );
      if ( proc == null )
        throw new InvalidOperationException( "Failed to start 'arp' process." );

      while ( !proc.StandardOutput.EndOfStream ) {
        var line = proc.StandardOutput.ReadLine();
        //Console.WriteLine( line );
        if ( string.IsNullOrWhiteSpace( line ) ) continue;
        if ( line.StartsWith( "Address" ) ) continue; // skip header

        var parts = line.Split( (char[]?) null, StringSplitOptions.RemoveEmptyEntries );

        // Defensive: expects at least Address, HWtype, HWaddress
        if ( parts.Length >= 3 &&
             parts[0].Count( c => c == '.' ) == 3 && // Looks like an IP
             parts[2].Contains( ':' ) ) // Looks like a MAC
        {
          var ip = parts[0];
          var mac = parts[2].ToUpperInvariant();
          map[ip] = mac;
        }
      }

      return map;
    }
  }
}