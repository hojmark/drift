using Grpc.Core;
using GrpcServerApp;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;

internal class ChatService : GrpcServerApp.ChatService.ChatServiceBase {
  public override async Task ChatStream(
    IAsyncStreamReader<ChatMessage> requestStream,
    IServerStreamWriter<ChatMessage> responseStream,
    ServerCallContext context 
    ) {
    var userName = "";

    // Start a background task to read incoming messages
    var readTask = Task.Run( async () => {
      await foreach ( var message in requestStream.ReadAllAsync(context.CancellationToken) ) {
        userName = message.User; // Save user for demo purposes
        Console.WriteLine( $"Received from {message.User}: {message.Message}" );
      }
    } );

    // While the client is connected, keep sending responses
    while ( !context.CancellationToken.IsCancellationRequested ) {
      if ( !string.IsNullOrEmpty( userName ) ) {
        await responseStream.WriteAsync( new ChatMessage {
          User = "Server", Message = $"Hello {userName}, the time is {DateTime.Now:T}"
        } );
      }

      await Task.Delay( 3000 ); // simulate work
    }

    await readTask;
  }
}