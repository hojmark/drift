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

    // Partition subnets by source
    var subnetsBySource = resolvedSubnets
      .Where( rs => options.Cidrs.Contains( rs.Cidr ) )
      .GroupBy( rs => rs.Source )
      .ToList();

    var allSubnetResults = new List<SubnetScanResult>();
    var startTime = DateTime.UtcNow;

    // Scan each group (local or per-agent)
    foreach ( var group in subnetsBySource ) {
      var cidrs = group.Select( rs => rs.Cidr ).ToList();

      if ( group.Key is Local ) {
        logger.LogDebug( "Scanning {Count} local subnet(s)", cidrs.Count );
        var localOptions = new NetworkScanOptions {
          Cidrs = cidrs,
          PingsPerSecond = options.PingsPerSecond
        };

        // Subscribe to local scanner progress and forward
        EventHandler<NetworkScanResult> localProgressHandler = ( _, result ) => {
          // Merge with current overall progress
          var overallResult = BuildOverallResult( allSubnetResults, result.Subnets, startTime );
          ResultUpdated?.Invoke( this, overallResult );
        };

        try {
          localScanner.ResultUpdated += localProgressHandler;
          var localResult = await localScanner.ScanAsync( localOptions, logger, cancellationToken );
          allSubnetResults.AddRange( localResult.Subnets );
        }
        finally {
          localScanner.ResultUpdated -= localProgressHandler;
        }
      }
      else if ( group.Key is Scanning.Subnets.Agent agentSource ) {
        logger.LogDebug(
          "Scanning {Count} subnet(s) via agent {AgentId}",
          cidrs.Count,
          agentSource.AgentId
        );

        // For now, scan one subnet at a time per agent
        // TODO: Parallel scanning of multiple subnets per agent
        foreach ( var cidr in cidrs ) {
          try {
            var result = await cluster.ScanSubnetAsync(
              // TODO: Need to map AgentId back to Domain.Agent
              new Domain.Agent {
                Id = agentSource.AgentId.ToString().Replace( "agentid_", string.Empty ), // TODO: Fix this mapping
                Address = string.Empty // TODO: Need proper agent lookup
              },
              cidr,
              options.PingsPerSecond,
              converter,
              progressUpdate => {
                logger.LogDebug(
                  "Agent {AgentId} scan progress for {Cidr}: {Progress}%",
                  agentSource.AgentId,
                  cidr,
                  progressUpdate.ProgressPercentage
                );

                // TODO: Build partial SubnetScanResult from progress and emit ResultUpdated
              },
              cancellationToken
            );

            allSubnetResults.Add( result.Result );
          }
          catch ( Exception ex ) {
            logger.LogWarning(
              ex,
              "Failed to scan subnet {Cidr} via agent {AgentId}",
              cidr,
              agentSource.AgentId
            );

            // Add a failed result
            allSubnetResults.Add( new SubnetScanResult {
              CidrBlock = cidr,
              DiscoveredDevices = [],
              Metadata = new Metadata { StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow },
              Status = ScanResultStatus.Error
            } );
          }
        }
      }
    }

    var endTime = DateTime.UtcNow;
    var finalResult = new NetworkScanResult {
      Subnets = allSubnetResults,
      Metadata = new Metadata { StartedAt = startTime, EndedAt = endTime },
      Status = allSubnetResults.All( s => s.Status == ScanResultStatus.Success )
        ? ScanResultStatus.Success
        : ScanResultStatus.Error,
      Progress = Percentage.Hundred
    };

    ResultUpdated?.Invoke( this, finalResult );

    logger.LogInformation(
      "Distributed scan completed: {SuccessCount}/{TotalCount} subnets successful",
      allSubnetResults.Count( s => s.Status == ScanResultStatus.Success ),
      allSubnetResults.Count
    );

    return finalResult;
  }

  private static NetworkScanResult BuildOverallResult(
    List<SubnetScanResult> completedSubnets,
    IReadOnlyCollection<SubnetScanResult> inProgressSubnets,
    DateTime startTime
  ) {
    var allSubnets = completedSubnets.Concat( inProgressSubnets ).ToList();
    var totalSubnets = allSubnets.Count;
    var completedCount = completedSubnets.Count;

    // Calculate overall progress
    var overallProgress = totalSubnets > 0
      ? (byte) ( completedCount * 100 / totalSubnets )
      : (byte) 0;

    return new NetworkScanResult {
      Subnets = allSubnets,
      Metadata = new Metadata { StartedAt = startTime, EndedAt = DateTime.UtcNow },
      Status = ScanResultStatus.InProgress,
      Progress = new Percentage( overallProgress )
    };
  }
}
