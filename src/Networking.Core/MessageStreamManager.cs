using Drift.Domain;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Grpc.Generated;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Core;

/// <inheritdoc />
/// <remarks>
/// The manager reuses active streams where possible and owns the lifetime of streams it creates or accepts.
/// Disposing the manager closes all streams it currently manages.
/// </remarks>
internal sealed class MessageStreamManager(
  ILogger logger,
  IMessagingClientFactory messageClientFactory,
  IServiceScopeFactory scopeFactory,
  MessagingOptions options
) : IMessageStreamManager {
  private readonly Dictionary<AgentId, Connection> _connections = new();
  private readonly Lock _lock = new();

  public event Action<AgentId, ConnectionCloseOrigin>? ConnectionClosed;

  /// <inheritdoc />
  public IMessageStreamConnection GetOrCreate( Uri peerAddress, AgentId id ) {
    logger.LogDebug(
      "Getting or creating {ConnectionSide} stream to agent {Id} ({Address})",
      ConnectionSide.Outbound,
      id,
      peerAddress
    );

    Connection connection;

    lock ( _lock ) {
      if ( _connections.TryGetValue( id, out var existing ) ) {
        if ( !existing.Completion.IsCompleted ) {
          return existing;
        }

        _connections.Remove( id );
      }

      connection = Connection.CreateOutbound(
        peerAddress,
        id,
        messageClientFactory,
        scopeFactory,
        logger,
        options
      );
      _connections[id] = connection;
    }

    _ = HandleCompletionAsync( connection );

    return connection;
  }

  /// <inheritdoc />
  public IMessageStreamConnection Create(
    IAsyncStreamReader<Message> requestStream,
    IAsyncStreamWriter<Message> responseStream,
    ServerCallContext context
  ) {
    var connection = Connection.CreateInbound(
      requestStream,
      responseStream,
      context,
      scopeFactory,
      logger,
      options
    );

    Connection? replaced;
    lock ( _lock ) {
      if ( _connections.TryGetValue( connection.Stream.RemoteId, out replaced ) ) {
        logger.LogWarning(
          "Replacing duplicate {ConnectionSide} stream for remote {Id} (stream #{StreamNo})",
          connection.Stream.Side,
          connection.Stream.RemoteId,
          replaced.Stream.InstanceNo
        );
      }

      _connections[connection.Stream.RemoteId] = connection;
    }

    if ( replaced is not null ) {
      _ = replaced.DisposeAsync().AsTask();
    }

    _ = HandleCompletionAsync( connection );

    return connection;
  }

  private async Task HandleCompletionAsync( Connection connection ) {
    await connection.Completion;

    try {
      lock ( _lock ) {
        if ( _connections.TryGetValue( connection.Stream.RemoteId, out var current ) &&
             ReferenceEquals( current, connection ) ) {
          _connections.Remove( connection.Stream.RemoteId );
        }
      }

      ConnectionClosed?.Invoke( connection.Stream.RemoteId, connection.ClosureOrigin );
    }
    finally {
      await connection.DisposeAfterCompletionAsync();
    }
  }

  public async ValueTask DisposeAsync() {
    Connection[] disposable;

    lock ( _lock ) {
      disposable = _connections.Values.ToArray();
      _connections.Clear();
    }

    logger.LogDebug( "Disposing stream manager (including all streams)" );
    foreach ( var connection in disposable ) {
      logger.LogTrace( "Disposing stream #{StreamNo}", connection.Stream.InstanceNo );
      await connection.DisposeAsync();
    }
  }
}