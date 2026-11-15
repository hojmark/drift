using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Runtime.Versioning;
using Drift.Common;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Scanners;

[SupportedOSPlatform( "linux" )]
internal sealed class LinuxFpingSubnetScanner : ISubnetScanner {
  public event EventHandler<SubnetScanResult>? ResultUpdated;

  public async Task<SubnetScanResult> ScanAsync(
    SubnetScanOptions options,
    ILogger logger,
    CancellationToken cancellationToken = default
  ) {
    var ipRange = IPNetwork2
      .Parse( options.Cidr.ToString() )
      .ListIPAddress( Filter.Usable )
      .Select( ip => ip )
      .ToList();

    var startedAt = DateTime.Now;

    var stopwatch = new Stopwatch();
    stopwatch.Start();

    var pingInterval = 1000 / options.PingsPerSecond;

    var discoveredDevices = new List<DiscoveredDevice>();

    var pingTool = new ToolWrapper( "fping" );
    pingTool.OutputDataReceived += data => {
      if ( string.IsNullOrEmpty( data ) ) {
        return;
      }

      discoveredDevices.Add( new DiscoveredDevice {
        Addresses = new DeviceAddressSet( ip: new IpV4Address( data ) ).ToAddresses()
      } );
      var elapsed = stopwatch.Elapsed;
      var estimatedScanned = elapsed.TotalSeconds * options.PingsPerSecond;
      var progress = Math.Min( 99, Math.Ceiling( ( estimatedScanned / ipRange.Count ) * 100 ) );
      ResultUpdated?.Invoke(
        this,
        new SubnetScanResult {
          Metadata = new Metadata { StartedAt = startedAt },
          CidrBlock = options.Cidr,
          Status = ScanResultStatus.InProgress,
          DiscoveredDevices = discoveredDevices,
          DiscoveryAttempts = discoveredDevices
            .SelectMany( device => device.Addresses.OfType<IpV4Address>() )
            .ToImmutableHashSet(),
          Progress = (Percentage) progress
        } );
    };

    var fping = await pingTool.ExecuteAsync(
      $"--alive --interval={pingInterval} --generate {options.Cidr}",
      logger,
      cancellationToken
    );

    // Exit code 1 means the scan completed but some hosts did not reply (which is very expected)
    var status = fping.Cancelled
      ? ScanResultStatus.Canceled
      : fping.ExitCode <= 1
        ? ScanResultStatus.Success
        : ScanResultStatus.Error;

    string? errorMessage = null;
    if ( status == ScanResultStatus.Error ) {
      var errorDetails = string.IsNullOrWhiteSpace( fping.ErrOut )
        ? string.Empty
        : $" {fping.ErrOut.Trim()}";
      errorMessage = $"fping failed with exit code {fping.ExitCode}.{errorDetails}";
      logger.LogError( "{ErrorMessage}", errorMessage );
    }

    var discoveryAttempts = discoveredDevices
      .SelectMany( device => device.Addresses.OfType<IpV4Address>() )
      .ToImmutableHashSet();

    if ( status == ScanResultStatus.Success ) {
      discoveryAttempts = ipRange.Select( ip => new IpV4Address( ip ) ).ToImmutableHashSet();
    }

    var result = new SubnetScanResult {
      Metadata = new Metadata { StartedAt = startedAt, EndedAt = DateTime.Now },
      Status = status,
      CidrBlock = options.Cidr,
      DiscoveredDevices = discoveredDevices,
      DiscoveryAttempts = discoveryAttempts,
      ErrorMessage = errorMessage
    };

    ResultUpdated?.Invoke( this, result );

    return result;
  }
}