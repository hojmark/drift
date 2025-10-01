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
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  ) {
    var ipRange = IPNetwork2
      .Parse( options.Cidr.ToString() )
      .ListIPAddress( Filter.Usable )
      .Select( ip => ip )
      .ToList();

    var startedAt = DateTime.Now;

    var pingInterval = 1000 / options.PingsPerSecond;

    var discoveredDevices = new List<DiscoveredDevice>();

    var pingTool = new ToolWrapper( "fping" );
    pingTool.OutputDataReceived += ( _, args ) => {
      if ( string.IsNullOrEmpty( args.Data ) ) return;
      discoveredDevices.Add( new DiscoveredDevice { Addresses = [new IpV4Address( args.Data )] } );
      var elapsed = DateTime.Now - startedAt;
      var estimatedScanned = elapsed.TotalSeconds * options.PingsPerSecond;
      var progress = Math.Min( 99, Math.Ceiling( ( estimatedScanned / ipRange.Count ) * 100 ) );
      ResultUpdated?.Invoke( this,
        new SubnetScanResult {
          Metadata = new Metadata { StartedAt = startedAt },
          CidrBlock = options.Cidr,
          Status = ScanResultStatus.InProgress,
          DiscoveredDevices = discoveredDevices,
          Progress = (Percentage) progress
        } );
    };

    var fping = await pingTool.ExecuteAsync(
      $"--alive --interval={pingInterval} --generate {options.Cidr}",
      logger,
      cancellationToken
    );

    if ( fping.ExitCode != 0 ) {
      //TODO
      //logger?.LogError( fping.ErrOut );
    }

    var result = new SubnetScanResult {
      Metadata = new Metadata { StartedAt = startedAt, EndedAt = DateTime.Now },
      Status = fping.ExitCode == 0 ? ScanResultStatus.Success : ScanResultStatus.Error,
      CidrBlock = options.Cidr,
      DiscoveredDevices = discoveredDevices
    };

    ResultUpdated?.Invoke( this, result );

    return result;
  }
}