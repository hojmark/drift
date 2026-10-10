using Drift.Coordinator.Services.Agents;
using Drift.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Drift.Coordinator.Host.Agents;

internal sealed class AgentConnectionMonitor : BackgroundService {
  private readonly IAgentDirectory _agentDirectory;
  private readonly AgentGateway _agentGateway;
  private readonly ILogger _logger;
  private readonly TimeSpan _interval;

  public AgentConnectionMonitor(
    IAgentDirectory agentDirectory,
    AgentGateway agentGateway,
    ILogger logger
  ) : this( agentDirectory, agentGateway, logger, TimeSpan.FromMinutes( 1 ) ) {
  }

  internal AgentConnectionMonitor(
    IAgentDirectory agentDirectory,
    AgentGateway agentGateway,
    ILogger logger,
    TimeSpan interval
  ) {
    _agentDirectory = agentDirectory;
    _agentGateway = agentGateway;
    _logger = logger;
    _interval = interval;
  }

  protected override async Task ExecuteAsync( CancellationToken stoppingToken ) {
    _logger.LogDebug( "Starting agent connection monitor" );

    using var timer = new PeriodicTimer( _interval );

    do {
      await CheckAllAgentsAsync( stoppingToken );
    } while ( await timer.WaitForNextTickAsync( stoppingToken ) );

    _logger.LogDebug( "Stopped agent connection monitor" );
  }

  private async Task CheckAllAgentsAsync( CancellationToken cancellationToken ) {
    var checks = _agentDirectory.GetEnrolledAgents().Select( agent => CheckAgentAsync( agent.Id, cancellationToken ) );
    await Task.WhenAll( checks );
  }

  private async Task CheckAgentAsync( AgentId agentId, CancellationToken cancellationToken ) {
    try {
      await _agentGateway.CheckStatusAsync( agentId, TimeSpan.FromSeconds( 1 ), cancellationToken );
    }
    catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
      throw;
    }
    catch ( Exception exception ) {
      _logger.LogTrace( exception, "Background connection check failed for agent {AgentId}", agentId );
    }
  }
}