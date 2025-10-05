using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.PeerStreaming.Server;

// Handle incoming connections (AKA server-side).
internal sealed class InboundPeerService( IPeerStreamManager peerStreamManager, ILogger logger )
  : PeerService.PeerServiceBase {
  public override async Task PeerStream(
    IAsyncStreamReader<PeerMessage> requestStream,
    IServerStreamWriter<PeerMessage> responseStream,
    ServerCallContext context
  ) {
    try {
      logger.LogInformation( "Inbound stream started" );
      var stream = peerStreamManager.Create( requestStream, responseStream, context );
      logger.LogInformation( "Peer stream #{StreamNo} created", stream.InstanceNo );

      // The stream is closed when the method returns.
      // We thus wait for the read loop to complete (meaning that this client is no longer interested in the stream).
      await stream.ReadTask;

      logger.LogInformation( "Peer stream #{StreamNo} completed", stream.InstanceNo );
    }
    catch ( Exception ex ) {
      logger.LogError( ex, "Inbound stream failed" );
    }
  }
}