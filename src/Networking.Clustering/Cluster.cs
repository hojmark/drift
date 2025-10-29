using Drift.Networking.PeerStreaming.Core;
using Drift.Networking.PeerStreaming.Core.Messages;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Clustering;

internal sealed class Cluster(
  IPeerMessageEnvelopeConverter envelopeConverter,
  PeerStreamManager peerStreamManager,
  PeerResponseAwaiter responseAwaiter,
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
    var envelope = envelopeConverter.ToEnvelope( message );
    await connection.SendAsync( envelope );
  }

  public async Task<TResponse> SendAndWaitAsync<TResponse>(
    Domain.Agent agent,
    IPeerMessage message,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default
  ) where TResponse : IPeerMessage {
    var correlationId = Guid.NewGuid().ToString();
    var envelope = envelopeConverter.ToEnvelope( message );
    envelope.CorrelationId = correlationId;

    // Register awaiter BEFORE sending
    var responseTask = responseAwaiter.WaitForResponseAsync(
      correlationId,
      timeout ?? TimeSpan.FromSeconds( 30 ),
      cancellationToken
    );

    // Send the request
    var connection = peerStreamManager.GetOrCreate( new Uri( agent.Address ), "agentid_local1" );
    await connection.SendAsync( envelope );

    // Wait for response
    var response = await responseTask;
    return envelopeConverter.FromEnvelope<TResponse>( response );
  }

  /* public async Task EnsureConnectedAsync( string peerAddress, CancellationToken cancellationToken = default ) {
     await peerStreamManager.GetOrCreateConnectionAsync( peerAddress, cancellationToken );
   }*/
}