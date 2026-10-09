using Drift.Domain;
using Drift.Networking.Grpc.Generated;
using Grpc.Core;

namespace Drift.Networking.Core.Abstractions;

/// <summary>
/// Manages messaging streams for remote agents.
/// </summary>
public interface IMessageStreamManager : IAsyncDisposable {
  /// <summary>
  /// Occurs once when a messaging connection ends.
  /// </summary>
  public event Action<AgentId, ConnectionCloseOrigin>? ConnectionClosed;

  /// <summary>
  /// Gets the existing stream for an agent, or creates an outbound stream to that agent.
  /// </summary>
  /// <param name="peerAddress">The address of the remote agent.</param>
  /// <param name="id">The identity of the remote agent.</param>
  /// <returns>The managed stream connection for the agent.</returns>
  public IMessageStreamConnection GetOrCreate( Uri peerAddress, AgentId id );

  /// <summary>
  /// Registers an inbound stream from a remote agent with the manager.
  /// </summary>
  /// <param name="requestStream">The stream used to receive messages from the remote agent.</param>
  /// <param name="responseStream">The stream used to send messages to the remote agent.</param>
  /// <param name="context">The server call context for the gRPC stream.</param>
  /// <returns>The managed stream connection for the agent.</returns>
  public IMessageStreamConnection Create(
    IAsyncStreamReader<Message> requestStream,
    IAsyncStreamWriter<Message> responseStream,
    ServerCallContext context
  );
}