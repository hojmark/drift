using Drift.Domain;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Networking.PeerStreaming.Core.Messages;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.PeerStreaming.Core;

public sealed class PeerStream : IPeerStream, IAsyncDisposable {
  private static int _instanceCounter;
  private readonly IAsyncStreamReader<PeerMessage> _reader;
  private readonly IAsyncStreamWriter<PeerMessage> _writer;
  private readonly PeerMessageDispatcher _dispatcher;
  private readonly ILogger _logger;
  private readonly CancellationToken _cancellationToken;
  private readonly AsyncDuplexStreamingCall<PeerMessage, PeerMessage>? _call;

  public int InstanceNo {
    get;
  } = Interlocked.Increment( ref _instanceCounter );

  private ConnectionSide Side {
    get;
  }

  private Uri? Address {
    get;
  }

  public required AgentId AgentId {
    get;
    init;
  }

  public Task ReadTask {
    get;
    private init;
  }

  public PeerStream(
    IAsyncStreamReader<PeerMessage> reader,
    IAsyncStreamWriter<PeerMessage> writer,
    PeerMessageDispatcher dispatcher,
    ILogger logger,
    CancellationToken cancellationToken
  ) {
    Side = ConnectionSide.Incoming;
    _reader = reader;
    _writer = writer;
    _dispatcher = dispatcher;
    _logger = logger;
    _cancellationToken = cancellationToken;
    ReadTask = Task.Run( ReadLoopAsync, CancellationToken.None );
  }

  public PeerStream(
    Uri address,
    AsyncDuplexStreamingCall<PeerMessage, PeerMessage> call,
    PeerMessageDispatcher dispatcher,
    ILogger logger,
    CancellationToken cancellationToken
  ) {
    Side = ConnectionSide.Outgoing;
    Address = address;
    _call = call;
    _reader = call.ResponseStream;
    _writer = call.RequestStream;
    _dispatcher = dispatcher;
    _logger = logger;
    _cancellationToken = cancellationToken;
    ReadTask = Task.Run( ReadLoopAsync, CancellationToken.None );
  }

  public async Task SendAsync( PeerMessage message ) {
    await _writer.WriteAsync( message, _cancellationToken );
  }

  private async Task ReadLoopAsync() {
    _logger.LogDebug( "Read loop starting..." );

    try {
      await foreach ( var message in _reader.ReadAllAsync( _cancellationToken ) ) {
        try {
          _logger.LogDebug( "Received message. Dispatching to handler..." );
          await _dispatcher.DispatchAsync( message, this, CancellationToken.None );
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
    Console.WriteLine( "Disposing " + this );

    if ( _call != null ) {
      // I.e., outgoing stream (client initiated)
      await _call.RequestStream.CompleteAsync();
    }

    await ReadTask;
  }

  public override string ToString() {
    return
      $"{nameof(PeerStream)}[#{InstanceNo}, {nameof(AgentId)}={AgentId}, {nameof(Side)}={Side}, {nameof(Address)}={Address}]";
  }
}