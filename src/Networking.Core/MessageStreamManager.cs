using System.Collections.Concurrent;
using Drift.Domain;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Core.Common;
using Drift.Networking.Core.Messages;
using Drift.Networking.Grpc.Generated;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Core;

internal sealed class MessageStreamManager(
  ILogger logger,
  IMessagingClientFactory? messageClientFactory,
  MessageDispatcher dispatcher,
  MessagingOptions options
) : IMessageStreamManager {
  private readonly ConcurrentDictionary<AgentId, IMessageStream> _streams = new();

  public IMessageStream GetOrCreate( Uri peerAddress, AgentId id ) {
    logger.LogDebug(
      "Getting or creating {ConnectionSide} stream to agent {Id} ({Address})",
      ConnectionSide.Outbound,
      id,
      peerAddress
    );

    lock ( _streams ) {
      if ( _streams.TryGetValue( id, out var existing ) ) {
        if ( !existing.ReadTask.IsCompleted ) {
          return existing;
        }

        _streams.TryRemove( new KeyValuePair<AgentId, IMessageStream>( id, existing ) );
        _ = existing.DisposeAsync().AsTask();
      }

      return Create( peerAddress, id );
    }
  }

  private IMessageStream Create( Uri peerAddress, AgentId id ) {
    if ( messageClientFactory == null ) {
      throw new Exception(
        $"Cannot create {nameof(ConnectionSide.Outbound)} stream since {nameof(messageClientFactory)} is null"
      );
    }

    var (client, _) = messageClientFactory.Create( peerAddress );
    var callOptions = new CallOptions( new Metadata { { "agent-id", id } } );
    var call = client.Connect( callOptions );

    var stream = new MessageStream(
      peerAddress,
      call.ResponseStream,
      call.RequestStream,
      dispatcher,
      logger,
      options.StoppingToken
    ) { RemoteId = id };
    Add( stream );
    return stream;
  }

  public IMessageStream Create(
    IAsyncStreamReader<Message> requestStream,
    IAsyncStreamWriter<Message> responseStream,
    ServerCallContext context
  ) {
    var agentId = context.RequestHeaders.GetAgentId();

    logger.LogInformation( "Creating {ConnectionSide} stream from agent {Id}", ConnectionSide.Inbound, agentId );

    var connectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(
      options.StoppingToken, // Drift shutdown
      context.CancellationToken // Client connection close
    );

    var stream = new MessageStream(
      requestStream,
      responseStream,
      dispatcher,
      logger,
      connectionCancellation.Token
    ) { RemoteId = agentId };

    Add( stream );

    // Dispose the linked token source after the stream's read loop has finished.
    _ = stream.ReadTask.ContinueWith(
      _ => connectionCancellation.Dispose(),
      CancellationToken.None
    );

    return stream;
  }

  private void Add( IMessageStream stream ) {
    logger.LogTrace( "Created {Stream}", stream );
    lock ( _streams ) {
      if ( _streams.TryGetValue( stream.RemoteId, out var previous ) && !ReferenceEquals( previous, stream ) ) {
        logger.LogWarning(
          "Replacing duplicate {ConnectionSide} stream for remote {Id} (stream #{StreamNo})",
          ConnectionSide.Inbound,
          stream.RemoteId,
          previous.InstanceNo
        );
        _ = previous.DisposeAsync().AsTask();
      }

      _streams[stream.RemoteId] = stream;
    }

    // Remove completed streams
    _ = stream.ReadTask.ContinueWith(
      _ => RemoveCompletedStream( stream ),
      CancellationToken.None
    );
  }

  private void RemoveCompletedStream( IMessageStream stream ) {
    if ( _streams.TryRemove( new KeyValuePair<AgentId, IMessageStream>( stream.RemoteId, stream ) ) ) {
      logger.LogDebug( "Removed completed stream #{StreamNo}", stream.InstanceNo );
    }
  }

  public async ValueTask DisposeAsync() {
    logger.LogDebug( "Disposing stream manager (including all streams)" );
    foreach ( var stream in _streams.Values ) {
      logger.LogTrace( "Disposing stream #{StreamNo}", stream.InstanceNo );
      await stream.DisposeAsync();
    }
  }
}