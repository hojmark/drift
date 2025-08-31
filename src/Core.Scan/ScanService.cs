using System.Drawing;
using Drift.Core.Abstractions;
using Drift.Core.Scan.Model;
using Drift.Core.Scan.Model.Progress;
using Drift.Core.Scan.Subnet;
using Drift.Domain;
using Drift.Domain.NeoProgress;
using Drift.Domain.Progress;
using Drift.Utils;
using Drift.Utils.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Core.Scan;

public class ScanService : IScanService {
  private readonly IInterfaceSubnetProvider _interfaceSubnetProvider;
  private readonly IPingTool _pingTool;

  public ScanService( IInterfaceSubnetProvider interfaceSubnetProvider, IPingTool pingTool ) {
    _interfaceSubnetProvider = interfaceSubnetProvider;
    _pingTool = pingTool;
  }

  public enum ScanStep {
    Init,
    PingScan,
    DNSResolution,
    ConnectScan
  }

  public async Task<ScanResponse> ScanAsync(
    ScanRequest request,
    Action<ProgressNode>? onProgress = null,
    CancellationToken cancellationToken = default
  ) {
    var network = request.Spec;

    var builder = new ProgressBuilder( onProgress );
    var discovery = builder.Root.Add( ScanPhase.Discovery.ToString() );
    discovery.Path = "Discovering subnets";
    var scanning = builder.Root.Add( ScanPhase.NetworkScanning.ToString() );
    scanning.Path = "Discovering devices";
    scanning.Weight = 99;

    // Phase 1: Discovery
    var subnets =
      await SubnetDiscovery.DetermineSubnets( request, discovery, _interfaceSubnetProvider );

    // Phase 2: Network Scanning
    //var devices = await ExecutePingScan( subnets, builder, onProgress );
    var d = await new PingNetworkScanner2( _pingTool ).ScanAsync(
      subnets,
      NullLogger.Instance,
      scanning,
      onProgressNew: onProgress, cancellationToken: cancellationToken );

    await Task.Delay( 500, cancellationToken );

    return new ScanResponse { Result = d };
  }
}

// High-level phases
public enum ScanPhase {
  Discovery,
  NetworkScanning,
  DeviceScanning,
  Analysis
}

// Steps within phases
public enum DiscoveryStep {
  SubnetDiscovery,
  ConfigurationLoading
}

public enum NetworkScanStep {
  PingScanning,
  ArpResolution
}

public enum DeviceScanStep {
  PortScanning,
  ServiceDetection
}

public enum AnalysisStep {
  ResultConsolidation,
  DriftDetection
}