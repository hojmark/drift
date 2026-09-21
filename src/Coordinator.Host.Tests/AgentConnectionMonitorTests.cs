using System.Collections.Concurrent;
using System.Diagnostics;
using Drift.Coordinator.Host.Agents;
using Drift.Coordinator.Host.Tests.Utils;
using Drift.Coordinator.Services.Agents;
using Drift.Coordinator.Services.Models;
using Drift.Domain;
using Drift.Messaging.Client;
using Drift.Messaging.Protocol.Agent.Status;
using Drift.Networking.Core.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Coordinator.Host.Tests;

internal sealed class AgentConnectionMonitorTests {
  [Test]
  public async Task ChecksAgentsRepeatedlyAndContinuesAfterFailure() {
    var readyId = AgentId.Parse( "agent_ready", null );
    var unavailableId = AgentId.Parse( "agent_unavailable", null );
    var directory = new InMemoryAgentDirectory( [CreateAgent( readyId ), CreateAgent( unavailableId )] );
    var client = new MonitorAgentClient( unavailableId );
    await using var streams = new FakeMessageStreamManager();
    using var gateway = new AgentGateway( client, directory, streams, NullLogger.Instance );
    var monitor = new AgentConnectionMonitor(
      directory,
      gateway,
      NullLogger.Instance,
      TimeSpan.FromMilliseconds( 10 )
    );

    await monitor.StartAsync( CancellationToken.None );
    try {
      await WaitForAsync(
        () => client.GetCallCount( readyId ) >= 2 && client.GetCallCount( unavailableId ) >= 2,
        TimeSpan.FromSeconds( 5 )
      );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( directory.GetConnectionStatus( readyId ), Is.EqualTo( AgentConnectionStatus.Connected ) );
        Assert.That( directory.GetConnectionStatus( unavailableId ), Is.EqualTo( AgentConnectionStatus.Unavailable ) );
      }
    }
    finally {
      await monitor.StopAsync( CancellationToken.None );
      monitor.Dispose();
    }
  }

  private static async Task WaitForAsync( Func<bool> condition, TimeSpan timeout ) {
    var started = Stopwatch.StartNew();
    while ( !condition() ) {
      if ( started.Elapsed >= timeout ) {
        Assert.Fail( "Condition was not met before the timeout." );
      }

      await Task.Delay( 10 );
    }
  }

  private static EnrolledAgent CreateAgent( AgentId id ) {
    return new EnrolledAgent( id, new Uri( "http://127.0.0.1:5001" ), DateTimeOffset.UtcNow );
  }

  private sealed class MonitorAgentClient( AgentId unavailableAgentId ) : IAgentClient {
    private readonly ConcurrentDictionary<AgentId, int> _callCounts = new();

    public int GetCallCount( AgentId agentId ) => _callCounts.GetValueOrDefault( agentId );

    public Task<TResponse> RequestAsync<TRequest, TResponse>(
      Domain.Agent agent,
      TRequest message,
      TimeSpan? timeout = null,
      CancellationToken cancellationToken = default
    ) where TResponse : IResponse where TRequest : IRequest<TResponse> {
      var agentId = AgentId.Parse( agent.Id, null );
      _callCounts.AddOrUpdate( agentId, 1, ( _, count ) => count + 1 );

      return agentId == unavailableAgentId
        ? Task.FromException<TResponse>( new InvalidOperationException( "Agent unavailable" ) )
        : Task.FromResult( (TResponse) (IResponse) new AgentStatusResponse { Status = AgentStatus.Ready } );
    }

    public Task<TResponse> RequestStreamingAsync<TRequest, TProgress, TResponse>(
      Domain.Agent agent,
      TRequest message,
      Action<TProgress> onProgress,
      TimeSpan? timeout = null,
      CancellationToken cancellationToken = default
    ) where TRequest : IStreamingRequest<TProgress, TResponse>
      where TProgress : IResponse
      where TResponse : IResponse {
      throw new NotSupportedException();
    }
  }
}