using System.Collections.Concurrent;
using Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;
using Drift.Domain;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.Grpc.Messages;
using Grpc.Core;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands.Peers;

public class PeerStreamManager(
  ILogger logger,
  IPeerClientFactory peerClientFactory,
  PeerMessageDispatcher dispatcher
) {
  private readonly ConcurrentDictionary<AgentId, PeerStream> _streams = new();

  public PeerStream GetOrCreate( Uri peerAddress, string id ) {
    var agentId = new AgentId( id );

    logger.LogInformation( "Getting or creating stream to agent {AgentId}", agentId );

    return _streams.GetOrAdd( agentId, _ => Create( peerAddress, agentId ) );
  }

  private PeerStream Create( Uri peerAddress, AgentId agentId ) {
    var (client, _) = peerClientFactory.Create( peerAddress );
    var callOptions = new CallOptions( new Metadata { { "agent-id", agentId } } );
    var call = client.PeerStream( callOptions );

    var stream = new PeerStream( peerAddress, call, dispatcher, logger ) { AgentId = agentId };
    Add( stream );
    return stream;
  }

  public PeerStream Create(
    IAsyncStreamReader<PeerMessage> requestStream,
    IAsyncStreamWriter<PeerMessage> responseStream,
    ServerCallContext context
  ) {
    var agentId = context.RequestHeaders.GetAgentId();
    logger.LogInformation( "Creating stream to agent {AgentId}", agentId );

    var stream = new PeerStream( requestStream, responseStream, dispatcher, logger ) { AgentId = agentId };
    Add( stream );
    return stream;
  }

  private void Add( PeerStream stream ) {
    logger.LogDebug( "Created stream to agent {AgentId}", stream.AgentId );
    logger.LogTrace( stream.ToString() );

    _streams[stream.AgentId] = stream;
  }
}