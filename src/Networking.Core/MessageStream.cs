using Drift.Domain;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Core.Messages;
using Drift.Networking.Grpc.Generated;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Core;

public sealed class MessageStream : IMessageStream {
  // Being static is not ideal for testing with multiple Drift instances in the same process
  private static int _instanceCounter;
  private readonly IAsyncStreamReader<Message> _reader;
  private readonly IAsyncStreamWriter<Message> _writer;
  private readonly MessageDispatcher _dispatcher;
  private readonly ILogger _logger;
  private readonly CancellationToken _cancellationToken;

  public MessageStream(
    IAsyncStreamReader<Message> reader,
    IAsyncStreamWriter<Message> writer,
    MessageDispatcher dispatcher,
    ILogger logger,
    CancellationToken cancellationToken
  ) {
    Side = ConnectionSide.Inbound;
    _reader = reader;
    _writer = writer;
    _dispatcher = dispatcher;
    _logger = logger;
    _cancellationToken = cancellationToken;
    // The read loop is considering the cancellation token to ensure a clean shutdown. Don't pass it to the task.
    ReadTask = Task.Run( ReadLoopAsync, CancellationToken.None );
  }

  public MessageStream(
    Uri address,
    IAsyncStreamReader<Message> reader,
    IAsyncStreamWriter<Message> writer,
    MessageDispatcher dispatcher,
    ILogger logger,
    CancellationToken cancellationToken
  ) : this( reader, writer, dispatcher, logger, cancellationToken ) {
    Side = ConnectionSide.Outbound;
    Address = address;
  }

  public int InstanceNo {
    get;
  } = Interlocked.Increment( ref _instanceCounter );

  private ConnectionSide Side {
    get;
  }

  private Uri? Address {
    get;
  }

  public required AgentId RemoteId {
    get;
    init;
  }

  public Task ReadTask {
    get;
    private init;
  }

  public async Task SendAsync( Message message ) {
    await _writer.WriteAsync( message, _cancellationToken ); // TODO also take another cancellation token (combine)
  }

  private async Task ReadLoopAsync() {
    _logger.LogDebug( "Read loop starting (waiting)..." );

    try {
      await foreach ( var message in _reader.ReadAllAsync( _cancellationToken ) ) {
        try {
          // TODO ensure this is printed in the output
          using var scope = _logger.BeginScope(
            new Dictionary<string, object?> { ["RequestId"] = message.CorrelationId ?? "no-id" }
          );
          _logger.LogDebug( "Received message. Dispatching to handler..." );
          await _dispatcher.DispatchAsync( message, this, CancellationToken.None );
          _logger.LogDebug( "Dispatch completed. Waiting for next message..." );
        }
        catch ( Exception ex ) {
          _logger.LogError( ex, "Message dispatch failed" );
        }
      }

      _logger.LogDebug( "Read loop ended gracefully (end of stream)" );
    }
    catch ( OperationCanceledException ) {
      // Justification: exception is control flow, not an error
#pragma warning disable S6667
      _logger.LogDebug( "Read loop ended gracefully (cancelled)" );
#pragma warning restore S6667
    }
    catch ( Exception ex ) {
      _logger.LogError( ex, "Read loop failed" );
    }
  }

  public async ValueTask DisposeAsync() {
    _logger.LogTrace( "Disposing {MessageStream}", this );

    if ( _writer is IClientStreamWriter<Message> clientWriter ) {
      // I.e., outgoing stream (client initiated)
      // Server streams are automatically completed by the gRPC framework
      await clientWriter.CompleteAsync();
    }

    await ReadTask;
  }

  public override string ToString() {
    return
      $"{nameof(MessageStream)}[#{InstanceNo}, {nameof(RemoteId)}={RemoteId}, {nameof(Side)}={Side}, {nameof(Address)}={Address?.ToString() ?? "n/a"}]";
  }
}