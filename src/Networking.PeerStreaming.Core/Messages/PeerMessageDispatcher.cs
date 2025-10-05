using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.PeerStreaming.Core.Messages;

public sealed class PeerMessageDispatcher {
  private readonly PeerResponseCorrelator _responseCorrelator;
  private readonly IPeerMessageEnvelopeConverter _envelopeConverter;
  private readonly ILogger _logger;
  private readonly Dictionary<string, IPeerMessageHandler> _handlers;

  public PeerMessageDispatcher(
    IEnumerable<IPeerMessageHandler> handlers,
    IPeerMessageEnvelopeConverter envelopeConverter,
    PeerResponseCorrelator responseCorrelator,
    ILogger logger
  ) {
    _responseCorrelator = responseCorrelator;
    _envelopeConverter = envelopeConverter;
    _logger = logger;
    _handlers = handlers.ToDictionary( h => h.MessageType, StringComparer.OrdinalIgnoreCase );
  }

  public async Task DispatchAsync( PeerMessage message, PeerStream peerStream, CancellationToken ct = default ) {
    _logger.LogDebug( "Dispatching message: {Type}", message.MessageType );

    // If this is a response to a pending request, complete it
    if ( !string.IsNullOrEmpty( message.ReplyTo ) ) {
      if ( _responseCorrelator.TryCompleteResponse( message.ReplyTo, message ) ) {
        _logger.LogDebug( "Completed pending request: {CorrelationId}", message.ReplyTo );
      }
      else {
        _logger.LogWarning( "Ignoring response for unknown correlation ID: {CorrelationId}", message.ReplyTo );
      }

      return;
    }

    // Otherwise, dispatch to handler
    if ( _handlers.TryGetValue( message.MessageType, out var handler ) ) {
      var response = await handler.HandleAsync( message, _envelopeConverter, ct );

      if ( response != null ) {
        var responseEnvelope = _envelopeConverter.ToEnvelope( response );
        responseEnvelope.ReplyTo = message.CorrelationId;
        await peerStream.SendAsync( responseEnvelope );
      }

      return;
    }

    throw new NotImplementedException( "Unknown message type '" + message.MessageType + "'" );
  }
}