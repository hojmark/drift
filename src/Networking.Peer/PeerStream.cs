using Drift.Domain;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.Grpc.Messages;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Peer;

public sealed class PeerStream /* : IAsyncDisposable*/ {
  private static int InstanceCounter = 0;

  public int InstanceNo {
    get;
  } = Interlocked.Increment( ref InstanceCounter );

  private ConnectionDirection Direction {
    get;
  }

  private Uri? Address {
    get;
  }

  public required AgentId AgentId {
    get;
    init;
  }

  private readonly IAsyncStreamReader<PeerMessage> _reader;
  private readonly IAsyncStreamWriter<PeerMessage> _writer;
  private readonly PeerMessageDispatcher _dispatcher;
  private readonly ILogger _logger;

  public Task ReadTask {
    get;
    private init;
  }

  public PeerStream(
    IAsyncStreamReader<PeerMessage> reader,
    IAsyncStreamWriter<PeerMessage> writer,
    PeerMessageDispatcher dispatcher,
    ILogger logger
  ) {
    Direction = ConnectionDirection.Incoming;
    _reader = reader;
    _writer = writer;
    _dispatcher = dispatcher;
    _logger = logger;
    ReadTask = Task.Run( ReadLoopAsync, CancellationToken.None );
  }

  public PeerStream(
    Uri address,
    // TODO Dispose call at some point
    AsyncDuplexStreamingCall<PeerMessage, PeerMessage> call,
    PeerMessageDispatcher dispatcher,
    ILogger logger
  ) {
    Direction = ConnectionDirection.Outgoing;
    Address = address;
    _reader = call.ResponseStream;
    _writer = call.RequestStream;
    _dispatcher = dispatcher;
    _logger = logger;
    ReadTask = Task.Run( ReadLoopAsync, CancellationToken.None );
  }

  public async Task SendAsync( PeerMessage message ) {
    await _writer.WriteAsync( message );
  }

  private async Task ReadLoopAsync() {
    _logger.LogInformation( "Read loop starting" );

    try {
      await foreach ( var message in _reader.ReadAllAsync() ) {
        try {
          _logger.LogDebug( "Received message. Dispatching..." );
          await _dispatcher.DispatchAsync( message, CancellationToken.None );
        }
        catch ( Exception ex ) {
          _logger.LogError( ex, "Message dispatch failed" );
        }
      }

      _logger.LogInformation( "Read loop ended gracefully (end of stream)" );
    }
    catch ( Exception ex ) {
      _logger.LogError( ex, "Read loop failed" );
    }

    _logger.LogInformation( "Read loop ended" );
  }

  public async ValueTask DisposeAsync() {
    Console.WriteLine( "Closing streams!" );
    //await Task.WhenAll( _readTask );
  }

  public override string ToString() {
    return $"{nameof(PeerStream)}[#{InstanceNo}, AgentId={AgentId}, Direction={Direction}, Address={Address}]";
  }
}