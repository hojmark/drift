using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcServerApp;

var channel = GrpcChannel.ForAddress( "http://localhost:5000" ); // use your actual server address
var client = new ChatService.ChatServiceClient( channel );

try {
  await client.PingAsync( new Empty() );
  Console.WriteLine( "✅ Connected to chat. Type messages below." );
}
catch ( RpcException ex ) {
  Console.WriteLine( $"❌ Cannot connect to server: {ex.StatusCode} - {ex.Message}" );
  return;
}

using var call = client.ChatStream();

// Start background task to read server messages
var readTask = Task.Run( async () => {
  try {
    await foreach ( var message in call.ResponseStream.ReadAllAsync() ) {
      Console.WriteLine( $"[{message.User}]: {message.Message}" );
    }
  }
  catch ( RpcException ex ) when ( ex.StatusCode == StatusCode.Cancelled ) {
    Console.WriteLine( "Stream closed by server." );
  }
} );

// Read from console and send to server
while ( true ) {
  var text = Console.ReadLine();
  if ( string.IsNullOrWhiteSpace( text ) )
    break;

  await call.RequestStream.WriteAsync( new ChatMessage { User = Environment.UserName, Message = text } );
}

// Gracefully complete the stream
await call.RequestStream.CompleteAsync();
await readTask;

Console.WriteLine( "Disconnected." );