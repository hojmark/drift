using Drift.Cli.Commands.Preview.Agent.Subcommands.Peers;
using Drift.Networking.Grpc.Generated;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;

// Handle incoming connections (AKA server-side).
internal class InboundPeerService(
  PeerStreamManager peerStreamManager,
  ILogger logger
) : PeerService.PeerServiceBase {
  public override async Task PeerStream(
    IAsyncStreamReader<PeerMessage> requestStream,
    IServerStreamWriter<PeerMessage> responseStream,
    ServerCallContext context
  ) {
    logger.LogInformation( "Inbound stream started" );
    var stream = peerStreamManager.Create( requestStream, responseStream, context );
    logger.LogInformation( "Peer stream #{StreamNo} created. Awaiting completion...", stream.InstanceNo );

    // It looks like when this method returns, the stream is closed.
    // We thus wait for the read loop to complete.
    await stream.ReadTask;

    logger.LogInformation( "Peer stream #{StreamNo} completed", stream.InstanceNo );
  }
}