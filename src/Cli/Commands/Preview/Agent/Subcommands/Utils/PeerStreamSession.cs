using Drift.Networking.Grpc.Generated;
using Drift.Networking.Grpc.Messages;
using Grpc.Core;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;

internal static class PeerStreamSession {
  public static async Task Run(
    IAsyncStreamReader<PeerMessage> reader,
    IAsyncStreamWriter<PeerMessage> writer,
    PeerMessageHandlerDispatcher dispatcher,
    CancellationToken cancellationToken
  ) {
    // Example: reading and processing inbound messages
    var readTask = Task.Run( async () => {
      await foreach ( var message in reader.ReadAllAsync( cancellationToken ) ) {
        await dispatcher.DispatchAsync( message );
      }
    }, cancellationToken );

    // Example: responding or sending outbound messages (expand as needed)
    // await writer.WriteAsync(...);

    await readTask;
  }
}