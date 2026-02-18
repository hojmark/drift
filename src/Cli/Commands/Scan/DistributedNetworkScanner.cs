using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Networking.Cluster;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Scanning.Subnets;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Scan;

/// <summary>
/// Network scanner that delegates scanning to agents based on subnet source.
/// </summary>
internal sealed class DistributedNetworkScanner(
  INetworkScanner localScanner,
  ICluster cluster,
  IPeerMessageEnvelopeConverter converter,
  List<ResolvedSubnet> resolvedSubnets,
  ILogger logger
) : INetworkScanner {
  public event EventHandler<NetworkScanResult>? ResultUpdated;

  public async Task<NetworkScanResult> ScanAsync(
    NetworkScanOptions options,
    ILogger logger,
    CancellationToken cancellationToken = default
  ) {
    logger.LogDebug( "Starting distributed network scan" );

    var subnetsBySource = PartitionSubnetsBySource( options );
    var allSubnetResults = new List<SubnetScanResult>();
    var startTime = DateTime.UtcNow;

    foreach ( var (source, cidrs) in subnetsBySource ) {
      if ( source is Local ) {
        await ScanLocalSubnetsAsync( cidrs, options.PingsPerSecond, allSubnetResults, startTime, logger, cancellationToken );
      }
      else if ( source is Scanning.Subnets.Agent agentSource ) {
        await ScanAgentSubnetsAsync( agentSource, cidrs, options.PingsPerSecond, allSubnetResults, cancellationToken );
      }
    }

    return BuildFinalResult( allSubnetResults, startTime, logger );
  }

  private List<(SubnetSource Source, List<CidrBlock> Cidrs)> PartitionSubnetsBySource( NetworkScanOptions options ) {
    return resolvedSubnets
      .Where( rs => options.Cidrs.Contains( rs.Cidr ) )
      .GroupBy( rs => rs.Source )
      .Select( group => (group.Key, group.Select( rs => rs.Cidr ).ToList()) )
      .ToList();
  }

  private async Task ScanLocalSubnetsAsync(
    List<CidrBlock> cidrs,
    uint pingsPerSecond,
    List<SubnetScanResult> allResults,
    DateTime startTime,
    ILogger logger,
    CancellationToken cancellationToken
  ) {
    logger.LogDebug( "Scanning {Count} local subnet(s)", cidrs.Count );

    var localOptions = new NetworkScanOptions {
      Cidrs = cidrs,
      PingsPerSecond = pingsPerSecond
    };

    EventHandler<NetworkScanResult> progressHandler = ( _, result ) => {
      var overallResult = BuildProgressResult( allResults, result.Subnets, startTime );
      ResultUpdated?.Invoke( this, overallResult );
    };

    try {
      localScanner.ResultUpdated += progressHandler;
      var localResult = await localScanner.ScanAsync( localOptions, logger, cancellationToken );
      allResults.AddRange( localResult.Subnets );
    }
    finally {
      localScanner.ResultUpdated -= progressHandler;
    }
  }

  private async Task ScanAgentSubnetsAsync(
    Scanning.Subnets.Agent agentSource,
    List<CidrBlock> cidrs,
    uint pingsPerSecond,
    List<SubnetScanResult> allResults,
    CancellationToken cancellationToken
  ) {
    logger.LogDebug( "Scanning {Count} subnet(s) via agent {AgentId}", cidrs.Count, agentSource.AgentId );

    // TODO: Parallel scanning of multiple subnets per agent
    foreach ( var cidr in cidrs ) {
      var result = await ScanSingleSubnetViaAgentAsync( agentSource, cidr, pingsPerSecond, cancellationToken );
      allResults.Add( result );
    }
  }

  private async Task<SubnetScanResult> ScanSingleSubnetViaAgentAsync(
    Scanning.Subnets.Agent agentSource,
    CidrBlock cidr,
    uint pingsPerSecond,
    CancellationToken cancellationToken
  ) {
    try {
      var response = await cluster.ScanSubnetAsync(
        MapAgentIdToDomainAgent( agentSource.AgentId ),
        cidr,
        pingsPerSecond,
        converter,
        progressUpdate => LogAgentProgress( agentSource.AgentId, cidr, progressUpdate.ProgressPercentage ),
        cancellationToken
      );

      return response.Result;
    }
    catch ( Exception ex ) {
      logger.LogWarning( ex, "Failed to scan subnet {Cidr} via agent {AgentId}", cidr, agentSource.AgentId );
      return CreateFailedScanResult( cidr );
    }
  }

  private static Domain.Agent MapAgentIdToDomainAgent( AgentId agentId ) {
    // TODO: Fix this mapping - need proper agent lookup from inventory
    return new Domain.Agent {
      Id = agentId.ToString().Replace( "agentid_", string.Empty ),
      Address = string.Empty
    };
  }

  private void LogAgentProgress( AgentId agentId, CidrBlock cidr, byte progressPercentage ) {
    logger.LogDebug( "Agent {AgentId} scan progress for {Cidr}: {Progress}%", agentId, cidr, progressPercentage );
    // TODO: Emit progress updates via ResultUpdated event
  }

  private static SubnetScanResult CreateFailedScanResult( CidrBlock cidr ) {
    return new SubnetScanResult {
      CidrBlock = cidr,
      DiscoveredDevices = [],
      Metadata = new Metadata { StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow },
      Status = ScanResultStatus.Error
    };
  }

  private static NetworkScanResult BuildProgressResult(
    List<SubnetScanResult> completedSubnets,
    IReadOnlyCollection<SubnetScanResult> inProgressSubnets,
    DateTime startTime
  ) {
    var allSubnets = completedSubnets.Concat( inProgressSubnets ).ToList();
    var progress = CalculateProgress( completedSubnets.Count, allSubnets.Count );

    return new NetworkScanResult {
      Subnets = allSubnets,
      Metadata = new Metadata { StartedAt = startTime, EndedAt = DateTime.UtcNow },
      Status = ScanResultStatus.InProgress,
      Progress = progress
    };
  }

  private NetworkScanResult BuildFinalResult(
    List<SubnetScanResult> allResults,
    DateTime startTime,
    ILogger logger
  ) {
    var endTime = DateTime.UtcNow;
    var successCount = allResults.Count( s => s.Status == ScanResultStatus.Success );

    var finalResult = new NetworkScanResult {
      Subnets = allResults,
      Metadata = new Metadata { StartedAt = startTime, EndedAt = endTime },
      Status = successCount == allResults.Count ? ScanResultStatus.Success : ScanResultStatus.Error,
      Progress = Percentage.Hundred
    };

    ResultUpdated?.Invoke( this, finalResult );

    logger.LogInformation(
      "Distributed scan completed: {SuccessCount}/{TotalCount} subnets successful",
      successCount,
      allResults.Count
    );

    return finalResult;
  }

  private static Percentage CalculateProgress( int completed, int total ) {
    if ( total == 0 ) {
      return Percentage.Zero;
    }

    var progressValue = (byte) ( completed * 100 / total );
    return new Percentage( progressValue );
  }
}
