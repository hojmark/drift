using System.Collections.Concurrent;
using Drift.Domain;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Networking.PeerStreaming.Core.Common;
using Drift.Networking.PeerStreaming.Core.Messages;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.PeerStreaming.Core;

internal sealed class PeerStreamManager(
  ILogger logger,
  IPeerClientFactory? peerClientFactory,
  PeerMessageDispatcher dispatcher,
  PeerStreamingOptions options
) : IPeerStreamManager {
  private readonly ConcurrentDictionary<AgentId, IPeerStream> _streams = new();

  public IPeerStream GetOrCreate( Uri peerAddress, AgentId id ) {
    logger.LogDebug(
      "Getting or creating {ConnectionSide} stream to agent {Id} ({Address})",
      ConnectionSide.Outgoing,
      id,
      peerAddress
    );

    return _streams.GetOrAdd( id, agentId => Create( peerAddress, agentId ) );
  }

  private IPeerStream Create( Uri peerAddress, AgentId id ) {
    if ( peerClientFactory == null ) {
      throw new Exception( $"Cannot create outbound stream since {nameof(peerClientFactory)} is null" );
    }

    var (client, _) = peerClientFactory.Create( peerAddress );
    var callOptions = new CallOptions( new Metadata { { "agent-id", id } } );
    var call = client.PeerStream( callOptions );

    var stream = new PeerStream(
      peerAddress,
      call.ResponseStream,
      call.RequestStream,
      dispatcher,
      logger,
      options.StoppingToken
    ) { AgentId = id };
    Add( stream );
    return stream;
  }

  public IPeerStream Create(
    IAsyncStreamReader<PeerMessage> requestStream,
    IAsyncStreamWriter<PeerMessage> responseStream,
    ServerCallContext context
  ) {
    var agentId = context.RequestHeaders.GetAgentId();

    logger.LogInformation( "Creating {ConnectionSide} stream from agent {Id}", ConnectionSide.Incoming, agentId );

    var stream =
      new PeerStream( requestStream, responseStream, dispatcher, logger, options.StoppingToken ) { AgentId = agentId };
    Add( stream );
    return stream;
  }

  private void Add( IPeerStream stream ) {
    logger.LogTrace( "Created {Stream}", stream );
    _streams[stream.AgentId] = stream;
  }

  public async ValueTask DisposeAsync() {
    logger.LogDebug( "Disposing peer stream manager" );
    foreach ( var stream in _streams.Values ) {
      logger.LogTrace( "Disposing peer stream #{StreamNo}", stream.InstanceNo );
      await stream.DisposeAsync();
    }
  }
}