using Drift.Networking.Grpc.Generated;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.PeerStreaming.Inbound;

// Handle incoming connections (AKA server-side).
internal sealed class InboundPeerService( PeerStreamManager peerStreamManager, ILogger logger )
  : PeerService.PeerServiceBase {
  public override async Task PeerStream(
    IAsyncStreamReader<PeerMessage> requestStream,
    IServerStreamWriter<PeerMessage> responseStream,
    ServerCallContext context
  ) {
    logger.LogInformation( "Inbound stream started" );
    var stream = peerStreamManager.Create( requestStream, responseStream, context );
    logger.LogInformation( "Peer stream #{StreamNo} created", stream.InstanceNo );

    // It looks like when the method returns, the stream is closed.
    // We thus wait for the read loop to complete (meaning that this client is no longer interested in the stream).
    await stream.ReadTask; 

    logger.LogInformation( "Peer stream #{StreamNo} completed", stream.InstanceNo );
  }
}