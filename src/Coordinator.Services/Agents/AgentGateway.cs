using Drift.Common;
using Drift.Coordinator.Services.Models;
using Drift.Domain;
using Drift.Messaging.Client;
using Drift.Messaging.Protocol.Agent.Status;
using Drift.Networking.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Coordinator.Services.Agents;

/// <summary>
/// Provides the coordinator's communication boundary for enrolled agents.
/// </summary>
public sealed class AgentGateway : IDisposable {
  private readonly IAgentClient _client;
  private readonly IAgentDirectory _agentDirectory;
  private readonly ILogger _logger;
  private readonly IMessageStreamManager _messageStreamManager;

  public AgentGateway(
    IAgentClient client,
    IAgentDirectory agentDirectory,
    IMessageStreamManager messageStreamManager,
    ILogger logger
  ) {
    _client = client;
    _agentDirectory = agentDirectory;
    _messageStreamManager = messageStreamManager;
    _logger = logger;
    _messageStreamManager.ConnectionClosed += OnConnectionClosed;
  }

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
    using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );

    try {
      var request = _client.RequestAsync<AgentStatusRequest, AgentStatusResponse>(
        enrolledAgent.ToDomainAgent(),
        new AgentStatusRequest(),
        timeout,
        timeoutCancellation.Token
      );
      var response = await request.WaitAsync( timeout, cancellationToken );
      if ( response.Status != AgentStatus.Ready ) {
        throw new InvalidOperationException( $"Agent '{agentId}' is not ready." );
      }

      if ( response.Version != DriftMetadata.Version ) {
        _logger.LogWarning(
          "Agent '{AgentId}' is running version '{AgentVersion}', but coordinator is running version '{CoordinatorVersion}'. YMMV.",
          agentId,
          response.Version,
          DriftMetadata.Version
        );
      }

      MarkConnected( enrolledAgent );
    }
    catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
      throw;
    }
    catch ( TimeoutException exception ) {
      // TODO request stack cancellation on timeout should be owned by AgentClient!
      if ( !cancellationToken.IsCancellationRequested ) {
        await timeoutCancellation.CancelAsync();
      }

      var timeoutException = new TimeoutException(
        $"Status check for agent '{agentId}' timed out after {timeout}.",
        exception
      );
      MarkUnavailable( enrolledAgent, timeoutException );
      throw timeoutException;
    }
    catch ( Exception exception ) {
      MarkUnavailable( enrolledAgent, exception );
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
      var response = await _client.RequestAsync<TRequest, TResponse>(
        enrolledAgent.ToDomainAgent(),
        request,
        timeout,
        cancellationToken
      );
      MarkConnected( enrolledAgent );
      return response;
    }
    catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
      throw;
    }
    catch ( Exception exception ) {
      MarkUnavailable( enrolledAgent, exception );
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
      var response = await _client.RequestStreamingAsync<TRequest, TProgress, TResponse>(
        enrolledAgent.ToDomainAgent(),
        request,
        onProgress,
        timeout,
        cancellationToken
      );
      MarkConnected( enrolledAgent );
      return response;
    }
    catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
      throw;
    }
    catch ( Exception exception ) {
      MarkUnavailable( enrolledAgent, exception );
      throw;
    }
  }

  private void MarkConnected( EnrolledAgent agent ) {
    var previousStatus = _agentDirectory.MarkConnected( agent.Id );
    if ( previousStatus is not null and not AgentConnectionStatus.Connected ) {
      _logger.LogInformation(
        "Connection to agent {AgentId} at {Address} changed from {PreviousStatus} to {ConnectionStatus}",
        agent.Id,
        agent.Address,
        previousStatus,
        AgentConnectionStatus.Connected
      );
    }
  }

  private void MarkUnavailable( EnrolledAgent agent, Exception exception ) {
    var previousStatus = _agentDirectory.MarkUnavailable( agent.Id );
    if ( previousStatus is not null and not AgentConnectionStatus.Unavailable ) {
      _logger.LogWarning(
        "Connection to agent {AgentId} at {Address} changed from {PreviousStatus} to {ConnectionStatus}: {Reason}",
        agent.Id,
        agent.Address,
        previousStatus,
        AgentConnectionStatus.Unavailable,
        exception.GetBaseException().Message
      );
    }
  }

  private EnrolledAgent GetEnrolledAgent( AgentId agentId ) {
    if ( _agentDirectory.TryGet( agentId, out var enrolledAgent ) && enrolledAgent is not null ) {
      return enrolledAgent;
    }

    throw new KeyNotFoundException( $"Agent '{agentId}' was not found." );
  }

  private void OnConnectionClosed( AgentId agentId, ConnectionCloseOrigin origin ) {
    if ( origin == ConnectionCloseOrigin.Local ) {
      // Closing the connection locally doesn't indicate the agent is unavailable
      return;
    }

    if ( _agentDirectory.TryGet( agentId, out var enrolledAgent ) && enrolledAgent is not null ) {
      MarkUnavailable( enrolledAgent, new IOException( "The messaging connection was closed." ) );
    }
  }

  public void Dispose() {
    _messageStreamManager.ConnectionClosed -= OnConnectionClosed;
  }
}