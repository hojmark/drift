using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

public interface IPeerMessageHandler {
  /// <summary>
  /// Gets the message type name that this handler can process.
  /// </summary>
  string MessageType {
    get;
  }

  /// <summary>
  /// Handles an incoming peer message. The handler is responsible for sending response(s)
  /// via the provided stream. Can send multiple responses for streaming scenarios.
  /// </summary>
  Task HandleAsync(
    PeerMessage envelope,
    IPeerMessageEnvelopeConverter converter,
    IPeerStream stream,
    CancellationToken cancellationToken
  );
}