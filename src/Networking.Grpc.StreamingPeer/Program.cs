using Drift.Networking.Grpc.Generated;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

var channel = GrpcChannel.ForAddress( "http://localhost:51515" );
var client = new PeerService.PeerServiceClient( channel );

try {
  await client.PingAsync( new Empty() );
  Console.WriteLine( "Connected. Type messages below." );
}
catch ( RpcException ex ) {
  Console.WriteLine( $"Cannot connect to server: {ex.StatusCode} - {ex.Message}" );
  return;
}

using var call = client.PeerStream();

var readTask = Task.Run( async () => {
  try {
    await foreach ( var message in call.ResponseStream.ReadAllAsync() ) {
      Console.WriteLine( $"[]: {message.Message}" );
    }
  }
  catch ( RpcException ex ) when ( ex.StatusCode == StatusCode.Cancelled ) {
    Console.WriteLine( "Stream closed by server." );
  }
} );

// Read from console and send to server
while ( true ) {
  var text = Console.ReadLine();
  if ( string.IsNullOrWhiteSpace( text ) ) {
    break;
  }

  await call.RequestStream.WriteAsync( new PeerMessage { Message = text } );
}

// Gracefully complete the stream
await call.RequestStream.CompleteAsync();
await readTask;

Console.WriteLine( "Disconnected." );