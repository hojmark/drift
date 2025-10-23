using Drift.Cli.Commands.Agent.Subcommands.Peers;
using Drift.Networking.Grpc.Messages;

namespace Drift.Cli.Commands.Agent.Subcommands.Utils;

public class Cluster(
  IPeerMessageSerializer serializer,
  PeerStreamManager peerStreamManager,
  ILogger logger
) : ICluster {
  public async Task SendAsync(
    Domain.Agent agent,
    IPeerMessage message,
    CancellationToken cancellationToken = default
  ) {
    try {
      await SendInternalAsync( agent, message, cancellationToken );
    }
    catch ( Exception ex ) {
      logger.LogWarning( ex, "Send to {Peer} failed", agent );
    }
  }

  /* public async Task BroadcastAsync( PeerMessage message, CancellationToken cancellationToken = default ) {
     var peers = peerStreamManager.GetConnectedPeers();

     var tasks = peers.Select( async peer => {
       // TODO optimistically assume connection is alive, but automatically reconnect if it's not
       try {
         await SendInternalAsync( peer, message, cancellationToken );
       }
       catch ( Exception ex ) {
         logger.LogWarning( ex, "Broadcast to {Peer} failed", peer );
       }
     } );

     await Task.WhenAll( tasks );
   }*/

  public async Task SendInternalAsync(
    Domain.Agent agent,
    IPeerMessage message,
    CancellationToken cancellationToken = default
  ) {
    var connection = peerStreamManager.GetOrCreate( new Uri( agent.Address ), "agentid_local1" );
    var envelope = serializer.ToEnvelope( message );
    await connection.SendAsync( envelope );
  }

  /* public async Task EnsureConnectedAsync( string peerAddress, CancellationToken cancellationToken = default ) {
     await peerStreamManager.GetOrCreateConnectionAsync( peerAddress, cancellationToken );
   }*/
}