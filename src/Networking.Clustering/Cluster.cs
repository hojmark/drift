using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Networking.PeerStreaming.Core.Messages;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Clustering;

internal sealed class Cluster(
  IPeerMessageEnvelopeConverter envelopeConverter,
  IPeerStreamManager peerStreamManager,
  PeerResponseCorrelator responseCorrelator,
  ILogger logger
) : ICluster {
  /*public async Task SendAsync<TMessage>(
    Domain.Agent agent,
    TMessage message,
    CancellationToken cancellationToken = default
  ) where TMessage : IPeerMessage {
    try {
      await SendInternalAsync<TMessage>( agent, message, cancellationToken );
    }
    catch ( Exception ex ) {
      logger.LogWarning( ex, "Send to {Peer} failed", agent );
    }
  }*/

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

  /*public async Task SendInternalAsync<TMessage>(
    Domain.Agent agent,
    TMessage message,
    CancellationToken cancellationToken = default
  ) where TMessage : IPeerMessage {
    var connection = peerStreamManager.GetOrCreate( new Uri( agent.Address ), "agentid_local1" );
    var envelope = envelopeConverter.ToEnvelope<TMessage>( message );
    await connection.SendAsync( envelope );
  }*/

  public async Task<TResponse> SendAndWaitAsync<TReq, TResponse>(
    Domain.Agent agent,
    TReq message,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default
  ) where TResponse : IPeerMessage where TReq : IPeerMessage {
    var correlationId = Guid.NewGuid().ToString();
    var envelope = envelopeConverter.ToEnvelope<TReq>( message );
    envelope.CorrelationId = correlationId;

    // Register correlator BEFORE sending
    var responseTask = responseCorrelator.WaitForResponseAsync(
      correlationId,
      timeout ?? TimeSpan.FromSeconds( 30 ),
      cancellationToken
    );

    // Request
    var connection = peerStreamManager.GetOrCreate( new Uri( agent.Address ), "agentid_local1" );
    await connection.SendAsync( envelope );

    // Response
    var response = await responseTask;
    return envelopeConverter.FromEnvelope<TResponse>( response );
  }
}