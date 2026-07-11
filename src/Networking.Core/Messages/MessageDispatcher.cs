using Drift.Networking.Core.Abstractions;
using Drift.Networking.Grpc.Generated;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Core.Messages;

public sealed class MessageDispatcher {
  private readonly MessageResponseCorrelator _responseCorrelator;
  private readonly IMessageEnvelopeConverter _envelopeConverter;
  private readonly ILogger _logger;
  private readonly Dictionary<string, IMessageHandler> _handlers;

  public MessageDispatcher(
    IEnumerable<IMessageHandler> handlers,
    IMessageEnvelopeConverter envelopeConverter,
    MessageResponseCorrelator responseCorrelator,
    ILogger logger
  ) {
    _responseCorrelator = responseCorrelator;
    _envelopeConverter = envelopeConverter;
    _logger = logger;
    _handlers = handlers.ToDictionary( h => h.MessageType, StringComparer.OrdinalIgnoreCase );
  }

  public async Task DispatchAsync( Message message, MessageStream stream, CancellationToken ct = default ) {
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

    // Dispatch to handler - handler is responsible for sending response(s)
    if ( _handlers.TryGetValue( message.MessageType, out var handler ) ) {
      await handler.HandleAsync( message, _envelopeConverter, stream, ct );
      return;
    }

    throw new NotImplementedException( "Unknown message type '" + message.MessageType + "'" );
  }
}