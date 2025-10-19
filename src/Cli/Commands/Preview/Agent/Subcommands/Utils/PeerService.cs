using Drift.Networking.Grpc.Generated;
using Drift.Networking.Grpc.Messages;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;

// Handle incoming connections (AKA server-side).
internal class PeerService( PeerMessageHandlerDispatcher dispatcher, AgentId agentId )
  : Drift.Networking.Grpc.Generated.PeerService.PeerServiceBase {
  public override async Task PeerStream(
    IAsyncStreamReader<PeerMessage> requestStream,
    IServerStreamWriter<PeerMessage> responseStream,
    ServerCallContext context
  ) {
    await PeerStreamSession.Run( requestStream, responseStream, dispatcher, context.CancellationToken );
  }

  public override Task<PingResponse> Ping( Empty request, ServerCallContext context ) {
    return Task.FromResult( new PingResponse { PeerId = agentId.Value.ToString() } );
  }
}