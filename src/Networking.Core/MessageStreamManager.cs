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

    return _streams.GetOrAdd( id, agentId => Create( peerAddress, agentId ) );
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

    var stream =
      new MessageStream( requestStream, responseStream, dispatcher, logger, options.StoppingToken ) {
        RemoteId = agentId
      };
    Add( stream );
    return stream;
  }

  private void Add( IMessageStream stream ) {
    logger.LogTrace( "Created {Stream}", stream );
    _streams[stream.RemoteId] = stream;
  }

  public async ValueTask DisposeAsync() {
    logger.LogDebug( "Disposing stream manager (including all streams)" );
    foreach ( var stream in _streams.Values ) {
      logger.LogTrace( "Disposing stream #{StreamNo}", stream.InstanceNo );
      await stream.DisposeAsync();
    }
  }
}