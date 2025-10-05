using System.Threading.Channels;
using Grpc.Core;
using Channel = System.Threading.Channels.Channel;

namespace Drift.Networking.PeerStreaming.Tests.Helpers;

internal sealed record DuplexStreamEndpoint<TRequest, TResponse>( TRequest RequestStream, TResponse ResponseStream );

/// <summary>
/// Provides an in-memory bidirectional gRPC stream pair (one endpoint is the client, the other is the server).
/// </summary>
internal static class InMemoryDuplexStreamPair {
  public static (
    DuplexStreamEndpoint<IClientStreamWriter<TRequest>, IAsyncStreamReader<TResponse>> Client,
    DuplexStreamEndpoint<IAsyncStreamReader<TRequest>, IServerStreamWriter<TResponse>> Server
    )
    Create<TRequest, TResponse>( ServerCallContext serverContext ) where TRequest : class where TResponse : class {
    var clientToServer = Channel.CreateUnbounded<TRequest>();
    var serverToClient = Channel.CreateUnbounded<TResponse>();

    var server = new DuplexStreamEndpoint<IAsyncStreamReader<TRequest>, IServerStreamWriter<TResponse>>(
      new InMemoryServerStreamReader<TRequest>( clientToServer.Reader, serverContext ),
      new InMemoryServerStreamWriter<TResponse>( serverToClient.Writer, serverContext )
    );

    var client = new DuplexStreamEndpoint<IClientStreamWriter<TRequest>, IAsyncStreamReader<TResponse>>(
      new InMemoryStreamWriter<TRequest>( clientToServer.Writer ),
      new InMemoryStreamReader<TResponse>( serverToClient.Reader )
    );

    return ( client, server );
  }

  private sealed class InMemoryStreamWriter<T> : IClientStreamWriter<T> where T : class {
    private readonly ChannelWriter<T> _writer;

    public WriteOptions? WriteOptions {
      get;
      set;
    }

    internal InMemoryStreamWriter( ChannelWriter<T> writer ) {
      _writer = writer;
    }

    public async Task WriteAsync( T message ) {
      await _writer.WriteAsync( message );
    }

    public Task CompleteAsync() {
      _writer.Complete();
      return Task.CompletedTask;
    }
  }

  private sealed class InMemoryServerStreamReader<T> : InMemoryStreamReader<T> where T : class {
    private readonly ServerCallContext _context;

    internal InMemoryServerStreamReader( ChannelReader<T> reader, ServerCallContext context ) : base( reader ) {
      _context = context;
    }

    public override Task<bool> MoveNext( CancellationToken cancellationToken ) {
      _context.CancellationToken.ThrowIfCancellationRequested();
      return base.MoveNext( cancellationToken );
    }
  }

  private class InMemoryStreamReader<T> : IAsyncStreamReader<T> where T : class {
    private readonly ChannelReader<T> _reader;

    public T Current {
      get;
      private set;
    } = null!;

    internal InMemoryStreamReader( ChannelReader<T> reader ) {
      _reader = reader;
    }

    public virtual async Task<bool> MoveNext( CancellationToken cancellationToken ) {
      if ( await _reader.WaitToReadAsync( cancellationToken ) && _reader.TryRead( out var message ) ) {
        Current = message;
        return true;
      }

      Current = null!;
      return false;
    }
  }

  private sealed class InMemoryServerStreamWriter<T> : IServerStreamWriter<T> where T : class {
    private readonly ChannelWriter<T> _writer;
    private readonly ServerCallContext _context;

    public WriteOptions? WriteOptions {
      get {
        return new WriteOptions();
      }

      set {
        throw new NotSupportedException();
      }
    }

    internal InMemoryServerStreamWriter( ChannelWriter<T> writer, ServerCallContext context ) {
      _writer = writer;
      _context = context;
    }

    public Task WriteAsync( T message ) {
      _context.CancellationToken.ThrowIfCancellationRequested();

      if ( !_writer.TryWrite( message ) ) {
        throw new InvalidOperationException( "Unable to write message." );
      }

      return Task.CompletedTask;
    }
  }
}