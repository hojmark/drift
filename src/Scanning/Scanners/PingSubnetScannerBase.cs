using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.RateLimiting;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;
using Drift.Scanning.Arp;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Scanners;

internal abstract class PingSubnetScannerBase : ISubnetScanner {
  public event EventHandler<SubnetScanResult>? ResultUpdated;

  public async Task<SubnetScanResult> ScanAsync(
    SubnetScanOptions options,
    ILogger logger,
    CancellationToken cancellationToken = default
  ) {
    var pingReplies = new ConcurrentBag<( IPAddress Ip, bool Success, string? Hostname)>();
    var cidr = options.Cidr;
    var ipRange = IPNetwork2
      .Parse( cidr.ToString() )
      .ListIPAddress( Filter.Usable )
      .ToList();

    var total = ipRange.Count;
    var completed = 0u;
    var startedAt = DateTime.Now;

    if ( total == 0 ) {
      logger.LogWarning( "Skipping ping scan for CIDR block {Cidr} as it has no usable IP addresses", cidr );
      return new SubnetScanResult {
        Metadata = new Metadata { StartedAt = startedAt, EndedAt = startedAt },
        Status = ScanResultStatus.Canceled,
        CidrBlock = cidr
      };
    }

    logger.LogDebug( "Starting ping scan for CIDR block {Cidr} ({Total} addresses)", cidr, total );

    await using var rateLimiter = new TokenBucketRateLimiter( new TokenBucketRateLimiterOptions {
      AutoReplenishment = true,
      QueueLimit = int.MaxValue,
      QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
      ReplenishmentPeriod = TimeSpan.FromSeconds( 1 ),
      TokenLimit = (int) options.PingsPerSecond,
      TokensPerPeriod = (int) options.PingsPerSecond
    } );

    var pingTasks = ipRange.Select( async ip => {
      try {
        var lease = await rateLimiter.AcquireAsync( 1, cancellationToken );
        if ( !lease.IsAcquired ) {
          throw new Exception( "Could not acquire lease" );
        }

        var success = await PingAsync( ip, logger, cancellationToken );
        string? hostname = string.Empty;
        if ( success ) {
          logger.LogDebug( "Got reply from {Ip}", ip );
          hostname = await GetHostNameAsync( ip, 15 );
          // Console.WriteLine( hostname );
        }

        pingReplies.Add( ( ip, success, hostname ) );

        Interlocked.Increment( ref completed );

        var intermediateResult = new SubnetScanResult {
          Metadata = new Metadata { StartedAt = startedAt },
          Status = ScanResultStatus.InProgress,
          DiscoveredDevices = ToDiscoveredDevices( pingReplies, ArpTables().Cached ),
          DiscoveryAttempts = ToDiscoveryAttempts( ipRange, completed ),
          Progress = new((byte) Math.Ceiling( ( (double) completed / total ) * 100 )),
          CidrBlock = cidr
        };

        ResultUpdated?.Invoke( this, intermediateResult );
      }
      catch ( Exception e ) {
        logger.LogError( e, "Error while pinging {Ip}", ip );
      }
    } ).ToList();

    await Task.WhenAll( pingTasks );

    logger.LogDebug( "Reading ARP cache" );

    var arpTable = ArpTables().Fresh;

    var endedAt = DateTime.Now;

    Debug.Assert( completed == ipRange.Count, "Not all IPs were pinged" );

    var result = new SubnetScanResult {
      Metadata = new Metadata { StartedAt = startedAt, EndedAt = endedAt },
      Status = ScanResultStatus.Success,
      DiscoveredDevices = ToDiscoveredDevices( pingReplies, arpTable ),
      DiscoveryAttempts = ToDiscoveryAttempts( ipRange, (uint) ipRange.Count ),
      Progress = Percentage.Hundred,
      CidrBlock = cidr
    };

    ResultUpdated?.Invoke( this, result );

    logger?.LogDebug( "Finished ping scan for CIDR block {Cidr}", cidr );

    return result;
  }

  protected abstract Task<bool> PingAsync( IPAddress ip, ILogger logger, CancellationToken cancellationToken );

  protected abstract IArpTableProvider ArpTables();

  private static ImmutableHashSet<IpV4Address> ToDiscoveryAttempts( List<IPAddress> ipRange, uint completed ) {
    return ipRange.Take( (int) completed ).Select( ip => new IpV4Address( ip ) ).ToImmutableHashSet();
  }

  private static List<DiscoveredDevice> ToDiscoveredDevices(
    ConcurrentBag<( IPAddress Ip, bool Success, string? Hostname)> pingReplies,
    ArpTable arpTable
  ) {
    var localMacs = BuildLocalInterfaceMacTable();

    return pingReplies.Where( r => r.Success ).Select( pingReply =>
      new DiscoveredDevice { Addresses = CreateAddresses( pingReply ) }
    ).ToList();

    List<IDeviceAddress> CreateAddresses( ( IPAddress Ip, bool Success, string? Hostname) pingReply ) {
      var list = new List<IDeviceAddress> { new IpV4Address( pingReply.Ip ) };

      if ( !string.IsNullOrWhiteSpace( pingReply.Hostname ) ) {
        list.Add( new HostnameAddress( pingReply.Hostname ) );
      }

      if ( arpTable.TryGetValue( pingReply.Ip, out var mac ) ) {
        list.Add( mac );
      }
      else if ( localMacs.TryGetValue( pingReply.Ip, out var localMac ) ) {
        // ARP cache never contains the machine's own IPs. Fall back to reading
        // the MAC directly from the matching local interface.
        list.Add( localMac );
      }

      return list;
    }
  }

  /// <summary>
  /// Builds a map of local unicast IPv4 addresses to the MAC address of the
  /// interface that owns them. Used to resolve the MAC for the scanner's own
  /// IP addresses, which never appear in the ARP cache.
  /// </summary>
  private static Dictionary<IPAddress, MacAddress> BuildLocalInterfaceMacTable() {
    var map = new Dictionary<IPAddress, MacAddress>();

    foreach ( var iface in NetworkInterface.GetAllNetworkInterfaces() ) {
      var physicalAddress = iface.GetPhysicalAddress();
      if ( physicalAddress.GetAddressBytes().Length == 0 ) {
        continue; // loopback and tunnel interfaces have no MAC
      }

      var macString = string.Join( "-", physicalAddress.GetAddressBytes().Select( b => b.ToString( "X2" ) ) );

      foreach ( var unicast in iface.GetIPProperties().UnicastAddresses ) {
        if ( unicast.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ) {
          map[unicast.Address] = new MacAddress( macString );
        }
      }
    }

    return map;
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