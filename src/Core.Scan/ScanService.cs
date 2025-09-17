using System.Drawing;
using Drift.Core.Scan.Model;
using Drift.Core.Scan.Model.Progress;
using Drift.Core.Scan.Subnet;
using Drift.Domain;
using Drift.Domain.NeoProgress;
using Drift.Domain.Progress;
using Drift.Utils;
using Drift.Utils.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Core.Scan;

public class ScanService : IScanService {
  private readonly IInterfaceSubnetProvider _interfaceSubnetProvider;
  private readonly IPingTool _pingTool;

  public ScanService( IInterfaceSubnetProvider interfaceSubnetProvider, IPingTool pingTool ) {
    _interfaceSubnetProvider = interfaceSubnetProvider;
    _pingTool = pingTool;
  }

  public async Task<ScanResponse> ScanAsync(
    ScanRequest request,
    Action<ProgressNode>? onProgress = null,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  ) {
    var network = request.Spec;

    var progressRoot = ScanProgressFactory.Create( onProgress );
    var discovery = progressRoot.SubnetDiscovery;
    var scanning = progressRoot.DeviceDiscovery;

    // Phase 1: Discovery
    var subnets = await SubnetDiscovery
      .DetermineSubnets( request, discovery, _interfaceSubnetProvider, logger );

    discovery.Self.AssertComplete();

    logger?.LogInformation( "Found {Count} subnet(s) for scanning", subnets.Count );

    // Phase 2: Network Scanning
    //var devices = await ExecutePingScan( subnets, builder, onProgress );
    var scanResult = await new PingNetworkScanner2( _pingTool ).ScanAsync(
      subnets,
      logger,
      scanning,
      onProgressNew: onProgress,
      cancellationToken: cancellationToken
    );

    scanning.Self.AssertComplete();

    await Task.Delay( 500, cancellationToken );

    return new ScanResponse { Result = scanResult };
  }
}