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
  IMessagingClientFactory? messageClientFactory,
  IServiceScopeFactory scopeFactory,
  MessagingOptions options
) : IMessageStreamManager {
  private readonly Dictionary<AgentId, Connection> _connections = new();
  private readonly Lock _lock = new();

  /// <inheritdoc />
  public IMessageStreamConnection GetOrCreate( Uri peerAddress, AgentId id ) {
    logger.LogDebug(
      "Getting or creating {ConnectionSide} stream to agent {Id} ({Address})",
      ConnectionSide.Outbound,
      id,
      peerAddress
    );

    lock ( _lock ) {
      if ( _connections.TryGetValue( id, out var existing ) ) {
        if ( !existing.Completion.IsCompleted ) {
          return existing;
        }

        _connections.Remove( id );
        _ = CloseLocallyAsync( existing );
      }

      if ( messageClientFactory is null ) {
        throw new InvalidOperationException(
          $"Cannot create {nameof(ConnectionSide.Outbound)} stream since {nameof(messageClientFactory)} is null"
        );
      }

      var connection = Connection.CreateOutbound(
        peerAddress,
        id,
        messageClientFactory,
        scopeFactory,
        logger,
        options
      );
      Add( connection );
      return connection;
    }
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
    Add( connection );
    return connection;
  }

  private void Add( Connection connection ) {
    logger.LogTrace( "Created {Connection}", connection );
    lock ( _lock ) {
      if ( _connections.TryGetValue( connection.Stream.RemoteId, out var previous ) &&
           !ReferenceEquals( previous, connection ) ) {
        logger.LogWarning(
          "Replacing duplicate {ConnectionSide} stream for remote {Id} (stream #{StreamNo})",
          connection.Stream.Side,
          connection.Stream.RemoteId,
          previous.Stream.InstanceNo
        );
        _ = CloseLocallyAsync( previous );
      }

      _connections[connection.Stream.RemoteId] = connection;
    }

    _ = connection.Completion.ContinueWith(
      _ => RemoveCompleted( connection ),
      CancellationToken.None,
      TaskContinuationOptions.ExecuteSynchronously,
      TaskScheduler.Default
    );
  }

  private void RemoveCompleted( Connection connection ) {
    lock ( _lock ) {
      if ( !_connections.TryGetValue( connection.Stream.RemoteId, out var current ) ||
           !ReferenceEquals( current, connection ) ) {
        return;
      }

      _connections.Remove( connection.Stream.RemoteId );
    }

    _ = connection.DisposeAfterCompletionAsync().AsTask();
  }

  public async ValueTask DisposeAsync() {
    Connection[] connections;
    lock ( _lock ) {
      connections = _connections.Values.ToArray();
      _connections.Clear();
    }

    logger.LogDebug( "Disposing stream manager (including all streams)" );
    foreach ( var connection in connections ) {
      logger.LogTrace( "Disposing stream #{StreamNo}", connection.Stream.InstanceNo );
      await CloseLocallyAsync( connection );
    }
  }

  private static async Task CloseLocallyAsync( Connection connection ) {
    connection.MarkClosedLocally();
    await connection.DisposeAsync();
  }
}