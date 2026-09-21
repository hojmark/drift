using Drift.Domain;
using Drift.Messaging.Client;
using Drift.Messaging.Protocol.Agent.Status;
using Drift.Networking.Core.Abstractions;

namespace Drift.Coordinator.Services.Agents;

/// <summary>
/// Provides the coordinator's communication boundary for enrolled agents.
/// </summary>
public sealed class AgentGateway(
  IAgentClient client,
  IAgentDirectory agentDirectory
) {
  /// <summary>
  /// Checks an enrolled agent's availability by requesting its lightweight status.
  /// </summary>
  public async Task CheckStatusAsync( AgentId agentId, CancellationToken cancellationToken ) {
    await CheckStatusAsync( agentId, TimeSpan.FromSeconds( 10 ), cancellationToken );
  }

  /// <summary>
  /// Checks an enrolled agent's availability with a caller-supplied timeout.
  /// </summary>
  public async Task CheckStatusAsync(
    AgentId agentId,
    TimeSpan timeout,
    CancellationToken cancellationToken
  ) {
    var enrolledAgent = GetEnrolledAgent( agentId );

    try {
      var response = await client.RequestAsync<AgentStatusRequest, AgentStatusResponse>(
        enrolledAgent.ToDomainAgent(),
        new AgentStatusRequest(),
        timeout,
        cancellationToken
      );
      if ( response.Status != AgentStatus.Ready ) {
        throw new InvalidOperationException( $"Agent '{agentId}' is not ready." );
      }

      agentDirectory.MarkConnected( agentId );
    }
    catch {
      agentDirectory.MarkUnavailable( agentId );
      throw;
    }
  }

  /// <summary>
  /// Sends a typed request to an enrolled agent.
  /// </summary>
  /// <typeparam name="TRequest">The request type sent to the agent.</typeparam>
  /// <typeparam name="TResponse">The response type returned by the agent.</typeparam>
  public async Task<TResponse> RequestAsync<TRequest, TResponse>(
    AgentId agentId,
    TRequest request,
    TimeSpan timeout,
    CancellationToken cancellationToken
  ) where TRequest : IRequest<TResponse> where TResponse : IResponse {
    var enrolledAgent = GetEnrolledAgent( agentId );
    try {
      var response = await client.RequestAsync<TRequest, TResponse>(
        enrolledAgent.ToDomainAgent(),
        request,
        timeout,
        cancellationToken
      );
      agentDirectory.MarkConnected( agentId );
      return response;
    }
    catch {
      agentDirectory.MarkUnavailable( agentId );
      throw;
    }
  }

  /// <summary>
  /// Sends a typed streaming request to an enrolled agent and forwards its progress.
  /// </summary>
  /// <typeparam name="TRequest">The streaming request type sent to the agent.</typeparam>
  /// <typeparam name="TProgress">The progress response type.</typeparam>
  /// <typeparam name="TResponse">The final response type returned by the agent.</typeparam>
  public async Task<TResponse> RequestStreamingAsync<TRequest, TProgress, TResponse>(
    AgentId agentId,
    TRequest request,
    Action<TProgress> onProgress,
    TimeSpan timeout,
    CancellationToken cancellationToken
  ) where TRequest : IStreamingRequest<TProgress, TResponse>
    where TProgress : IResponse
    where TResponse : IResponse {
    var enrolledAgent = GetEnrolledAgent( agentId );
    try {
      var response = await client.RequestStreamingAsync<TRequest, TProgress, TResponse>(
        enrolledAgent.ToDomainAgent(),
        request,
        onProgress,
        timeout,
        cancellationToken
      );
      agentDirectory.MarkConnected( agentId );
      return response;
    }
    catch {
      agentDirectory.MarkUnavailable( agentId );
      throw;
    }
  }

  private EnrolledAgent GetEnrolledAgent( AgentId agentId ) {
    if ( agentDirectory.TryGet( agentId, out var enrolledAgent ) && enrolledAgent is not null ) {
      return enrolledAgent;
    }

    throw new KeyNotFoundException( $"Agent '{agentId}' was not found." );
  }
}