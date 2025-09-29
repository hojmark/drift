using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Scanners;

internal abstract class PingSubnetScannerBase : ISubnetScanner {
  public event EventHandler<SubnetScanResult>? ResultUpdated;

  protected abstract Task<bool> PingAsync( IPAddress ip, ILogger logger, CancellationToken cancellationToken );

  public async Task<SubnetScanResult> ScanAsync(
    SubnetScanOptions options,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  ) {
    var pingReplies = new ConcurrentBag<( IPAddress Ip, bool Success, string? Hostname)>();
    var cidr = options.Cidr;
    var ipRange = IPNetwork2
      .Parse( cidr.ToString() )
      .ListIPAddress( FilterEnum.Usable )
      .Select( ip => ip )
      .ToList();

    var total = ipRange.Count;
    var completed = 0u;

    if ( total == 0 ) {
      logger?.LogWarning( "Skipping ping scan for CIDR block {Cidr} as it has no usable IP addresses", cidr );
      return new SubnetScanResult { Metadata = null, Status = ScanResultStatus.Canceled, CidrBlock = cidr };
    }

    logger?.LogDebug( "Starting ping scan for CIDR block {Cidr} ({Total} addresses)", cidr, total );

    var startedAt = DateTime.Now;

    using var throttler = new SemaphoreSlim( (int) options.PingsPerSecond );

    var pingTasks = ipRange.Select( async ip => {
      await throttler.WaitAsync( cancellationToken );

      try {
        var success = await PingAsync( ip, logger, cancellationToken );
        string? hostname = "";
        if ( success ) {
          logger?.LogDebug( "Got reply from {Ip}", ip );
          hostname = await GetHostNameAsync( ip, 15 );
          //Console.WriteLine( hostname );
        }

        pingReplies.Add( ( ip, success, hostname ) );

        Interlocked.Increment( ref completed );

        if ( success ) {
          var arpCache = ArpHelper.GetSystemCachedIpToMacMap();
          var intermediateResult = new SubnetScanResult {
            Metadata = new Metadata { StartedAt = startedAt },
            Status = ScanResultStatus.InProgress,
            DiscoveredDevices = ToDiscoveredDevices( pingReplies, arpCache ),
            DiscoveryAttempts = ToDiscoveryAttemps( ipRange, completed ),
            Progress = new((byte) Math.Ceiling( ( (double) completed / total ) * 100 )),
            CidrBlock = cidr
          };

          ResultUpdated?.Invoke( null, intermediateResult );
        }
      }
      finally {
        _ = Task.Delay( (int) ( 1000u / options.PingsPerSecond ), cancellationToken )
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

    logger?.LogDebug( "Reading ARP cache" );

    var arpCache = ArpHelper.GetSystemCachedIpToMacMap();

    var endedAt = DateTime.Now;

    Debug.Assert( completed == ipRange.Count );

    var result = new SubnetScanResult {
      Metadata = new Metadata { StartedAt = startedAt, EndedAt = endedAt },
      Status = ScanResultStatus.Success,
      DiscoveredDevices = ToDiscoveredDevices( pingReplies, arpCache ),
      DiscoveryAttempts = ToDiscoveryAttemps( ipRange, (uint) ipRange.Count ),
      Progress = Percentage.Hundred,
      CidrBlock = cidr
    };

    ResultUpdated?.Invoke( null, result );

    logger?.LogDebug( "Finished ping scan for CIDR block {Cidr}", cidr );

    return result;
  }

  private static ImmutableHashSet<IpV4Address> ToDiscoveryAttemps( List<IPAddress> ipRange, uint completed ) {
    return ipRange.Take( (int) completed ).Select( ip => new IpV4Address( ip ) ).ToImmutableHashSet();
  }

  private static IEnumerable<DiscoveredDevice> ToDiscoveredDevices(
    ConcurrentBag<( IPAddress Ip, bool Success, string? Hostname)> pingReplies,
    Dictionary<IPAddress, string>? ipToMac
  ) {
    return pingReplies.Where( r => r.Success ).Select( pingReply =>
      new DiscoveredDevice { Addresses = CreateAddresses( pingReply ) }
    );

    List<IDeviceAddress> CreateAddresses( ( IPAddress Ip, bool Success, string? Hostname) pingReply ) {
      var list = new List<IDeviceAddress> { new IpV4Address( pingReply.Ip ) };

      if ( !string.IsNullOrWhiteSpace( pingReply.Hostname ) ) {
        list.Add( new HostnameAddress( pingReply.Hostname ) );
      }

      if ( ipToMac?.TryGetValue( pingReply.Ip, out var macStr ) ?? false ) {
        list.Add( new MacAddress( macStr ) );
      }

      return list;
    }
  }

  private static async Task<string?> GetHostNameAsync( IPAddress ip, int timeoutMs = 1000 ) {
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
}