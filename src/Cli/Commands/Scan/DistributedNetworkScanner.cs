using System.Collections.Immutable;
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
  Inventory inventory,
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
    // Allow each source to scan subnets it can see - don't deduplicate
    // Different agents may see different devices on the same subnet from their network position
    return resolvedSubnets
      .Where( rs => options.Cidrs.Contains( rs.Cidr ) )
      .GroupBy( rs => rs.Source )
      .Select( group => (group.Key, group.Select( rs => rs.Cidr ).Distinct().ToList()) )
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
    catch ( OperationCanceledException ) {
      logger.LogWarning( "Scan of subnet {Cidr} via agent {AgentId} was cancelled", cidr, agentSource.AgentId );
      return CreateFailedScanResult( cidr );
    }
    catch ( Exception ex ) {
      logger.LogWarning(
        ex,
        "Failed to scan subnet {Cidr} via agent {AgentId}: {ErrorMessage}. Returning partial results.",
        cidr,
        agentSource.AgentId,
        ex.Message
      );
      return CreateFailedScanResult( cidr );
    }
  }

  private Domain.Agent MapAgentIdToDomainAgent( AgentId agentId ) {
    var agentIdStr = agentId.ToString().Replace( "agentid_", string.Empty );
    var agent = inventory.Agents.FirstOrDefault( a => a.Id == agentIdStr );
    
    if ( agent == null ) {
      logger.LogWarning( "Agent {AgentId} not found in inventory", agentId );
      return new Domain.Agent { Id = agentIdStr, Address = string.Empty };
    }

    return agent;
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

    // Merge results for subnets that were scanned multiple times
    var mergedResults = MergeOverlappingSubnetResults( allResults, logger );
    var successCount = allResults.Count( s => s.Status == ScanResultStatus.Success );
    var failureCount = allResults.Count - successCount;

    var finalResult = new NetworkScanResult {
      Subnets = mergedResults,
      Metadata = new Metadata { StartedAt = startTime, EndedAt = endTime },
      Status = successCount == allResults.Count ? ScanResultStatus.Success : ScanResultStatus.Error,
      Progress = Percentage.Hundred
    };

    ResultUpdated?.Invoke( this, finalResult );

    // Log summary with warnings if there were failures
    if ( failureCount > 0 ) {
      var failedSubnets = allResults.Where( s => s.Status == ScanResultStatus.Error ).Select( s => s.CidrBlock ).ToList();
      logger.LogWarning(
        "Distributed scan completed with partial results: {SuccessCount}/{TotalCount} scan operations successful, {FailureCount} failed. Failed subnets: {FailedSubnets}",
        successCount,
        allResults.Count,
        failureCount,
        string.Join( ", ", failedSubnets )
      );
    }
    else {
      logger.LogInformation(
        "Distributed scan completed: {SuccessCount}/{TotalCount} scan operations successful, {UniqueSubnets} unique subnets",
        successCount,
        allResults.Count,
        mergedResults.Count
      );
    }

    return finalResult;
  }

  private List<SubnetScanResult> MergeOverlappingSubnetResults( List<SubnetScanResult> allResults, ILogger logger ) {
    // Group results by CIDR and merge devices from multiple scans
    var resultsByCidr = allResults
      .GroupBy( r => r.CidrBlock )
      .Select( group => {
        var cidr = group.Key;
        var scans = group.ToList();
        
        if ( scans.Count == 1 ) {
          return scans[0];
        }

        // Multiple scans of the same subnet - merge the results
        logger.LogDebug( "Merging {Count} scan results for subnet {Cidr}", scans.Count, cidr );

        // Combine all discovered devices, using device addresses as the key for deduplication
        var allDevices = scans
          .SelectMany( s => s.DiscoveredDevices )
          .GroupBy( d => string.Join( ",", d.Addresses.OrderBy( a => a.Value ).Select( a => a.Value ) ) )
          .Select( g => g.First() ) // Take first occurrence of each unique device
          .ToList();

        // Combine all discovery attempts
        var allAttempts = scans
          .SelectMany( s => s.DiscoveryAttempts )
          .ToImmutableHashSet();

        // Use the earliest start time and latest end time
        var startTime = scans.Min( s => s.Metadata.StartedAt );
        var endTime = scans.Max( s => s.Metadata.EndedAt );

        logger.LogInformation(
          "Merged {DeviceCount} unique devices from {ScanCount} scans of {Cidr}",
          allDevices.Count,
          scans.Count,
          cidr
        );

        return new SubnetScanResult {
          CidrBlock = cidr,
          DiscoveredDevices = allDevices,
          Metadata = new Metadata { StartedAt = startTime, EndedAt = endTime },
          Status = scans.All( s => s.Status == ScanResultStatus.Success ) ? ScanResultStatus.Success : ScanResultStatus.Error,
          DiscoveryAttempts = allAttempts
        };
      } )
      .ToList();

    return resultsByCidr;
  }

  private static Percentage CalculateProgress( int completed, int total ) {
    if ( total == 0 ) {
      return Percentage.Zero;
    }

    var progressValue = (byte) ( completed * 100 / total );
    return new Percentage( progressValue );
  }
}
