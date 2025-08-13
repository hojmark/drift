using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using Drift.Cli.Output.Abstractions;
using Drift.Cli.Tools;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Progress;
using Drift.Domain.Scan;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Scan;

internal class PingNetworkScanner( IOutputManager output, IPingTool pingTool ) : INetworkScanner {
  //TODO make private or configurable
  internal const int MaxPingsPerSecond = 50;

  public async Task<ScanResult> ScanAsync(
    List<CidrBlock> cidrs,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default,
    int maxPingsPerSecond = MaxPingsPerSecond
  ) {
    var logger = output.Log;
    var startedAt = DateTime.Now;
    logger.LogDebug( "Starting network scan at {StartedAt}", startedAt.ToString( CultureInfo.InvariantCulture ) );
    output.Normal.WriteLineVerbose( $"Starting network scan at {startedAt.ToString( CultureInfo.InvariantCulture )}" );

    onProgress?.Invoke( new ProgressReport {
      Tasks = [
        new TaskProgress { TaskName = "Ping Scan", CompletionPct = 0, },
        new TaskProgress { TaskName = "Indirect ARP Scan", CompletionPct = 0 }
      ]
    } );

    var pingReplies = new ConcurrentBag<(string Ip, bool Success, string? Hostname)>();

    foreach ( var cidr in cidrs ) {
      await PingScanAsync( pingReplies, cidr, output, maxPingsPerSecond, onProgress, cancellationToken );
    }

    logger.LogDebug( "Reading ARP cache" );

    // Note: reads from system ARP cache, which is assumed to be up to date after having performed a ping scan
    var ipToMac = ArpHelper.GetSystemCachedIpToMacMap();

    onProgress?.Invoke( new ProgressReport {
      Tasks = [
        new TaskProgress { TaskName = "Indirect ARP Scan", CompletionPct = 100 }
      ]
    } );

    var finishedAt = DateTime.Now;
    var elapsed = finishedAt - startedAt; // TODO .Humanize( 2, CultureInfo.InvariantCulture, minUnit: TimeUnit.Second )
    logger.LogDebug( "Finished network scan at {StartedAt} in {Elapsed}",
      finishedAt.ToString( CultureInfo.InvariantCulture ),
      elapsed
    );
    output.Normal.WriteLineVerbose(
      $"Finished network scan at {finishedAt.ToString( CultureInfo.InvariantCulture )} in {elapsed}"
    );

    return new ScanResult {
      DiscoveredDevices = ToDiscoveredDevices( pingReplies, ipToMac ),
      Metadata = new Metadata { StartedAt = startedAt, EndedAt = DateTime.Now },
      Status = ScanResultStatus.Success
    };
  }

  private async Task PingScanAsync( ConcurrentBag<(string Ip, bool Success, string? Hostname)> results,
    CidrBlock cidr,
    IOutputManager output,
    int maxPingsPerSecond,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default
  ) {
    var ipRange = IPNetwork2
      .Parse( cidr.ToString() )
      .ListIPAddress( FilterEnum.Usable )
      .Select( ip => ip.ToString() )
      .ToList();

    var total = ipRange.Count;
    var completed = 0;

    if ( total == 0 ) {
      output.Normal.WriteLineWarning(
        $"Skipping ping scan for CIDR block {cidr} as it has no usable IP addresses"
      );

      return;
    }

    output.Normal.WriteLineVerbose( $"Starting ping scan for CIDR block {cidr} ({total} addresses)" );

    using var throttler = new SemaphoreSlim( maxPingsPerSecond );

    var pingTasks = ipRange.Select( async ip => {
      await throttler.WaitAsync( cancellationToken );

      try {
        var success = ( await pingTool.RunAsync( $"-c 1 -W 1 {ip}" ) ).ExitCode == 0;
        string? hostname = "";
        if ( success ) {
          output.Normal.WriteLineVerbose( $"Got reply from {ip}" );
          hostname = await GetHostNameAsync( ip, 15 );
          //Console.WriteLine( hostname );
        }

        results.Add( ( ip, success, hostname ) );

        Interlocked.Increment( ref completed );

        onProgress?.Invoke( new ProgressReport {
          Tasks = [
            new TaskProgress {
              TaskName = "Ping Scan", CompletionPct = (int) Math.Ceiling( ( (double) completed / total ) * 100 )
            }
          ]
        } );
      }
      finally {
        _ = Task.Delay( 1000 / maxPingsPerSecond, cancellationToken )
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

    output.Normal.WriteLineVerbose( $"Finished ping scan for CIDR block {cidr}" );
  }

  private static IEnumerable<DiscoveredDevice> ToDiscoveredDevices(
    ConcurrentBag<(string Ip, bool Success, string? Hostname)> pingReplies, Dictionary<string, string> ipToMac ) {
    return pingReplies
      .Where( r => r.Success )
      .Select( pingReply =>
        new DiscoveredDevice { Addresses = CreateAddresses( pingReply ) }
      );

    List<IDeviceAddress> CreateAddresses( (string Ip, bool Success, string? Hostname) pingReply ) {
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