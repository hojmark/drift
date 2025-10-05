using Drift.Networking.Grpc.Generated;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;

internal class PeerService : Drift.Networking.Grpc.Generated.PeerService.PeerServiceBase {
  public override async Task PeerStream(
    IAsyncStreamReader<PeerMessage> requestStream,
    IServerStreamWriter<PeerMessage> responseStream,
    ServerCallContext context
  ) {
    var userName = "";

    // Start a background task to read incoming messages
    var readTask = Task.Run( async () => {
        await foreach ( var message in requestStream.ReadAllAsync( context.CancellationToken ) ) {
          userName = message.PeerId; // Save user for demo purposes
          Console.WriteLine( $"Received from {message.PeerId}: {message.Message}" );
        }
      }
    );

    // While the client is connected, keep sending responses
    while ( !context.CancellationToken.IsCancellationRequested ) {
      if ( !string.IsNullOrEmpty( userName ) ) {
        await responseStream.WriteAsync( new PeerMessage {
          PeerId = "Server", Message = $"Hello {userName}, the time is {DateTime.Now:T}"
        } );
      }

      await Task.Delay( 3000 ); // simulate work
    }

    await readTask;
  }

  public override Task<Empty> Ping( Empty request, ServerCallContext context ) {
    return Task.FromResult( new Empty() );
  }
}