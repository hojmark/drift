using Drift.Networking.GrpcP2P;
using Grpc.Core;
using Grpc.Net.Client;

namespace Drift.Networking.GrpcPeer;

sealed class Program {
  static async Task Main( string[] args ) {
    int MyPort = int.Parse( args[0] );
    int PeerPort = int.Parse( args[1] );

    // Start local server
    _ = Task.Run( () => StartServer( MyPort ) );

    await Task.Delay( 1000 ); // Wait for server to be ready

    var channel = GrpcChannel.ForAddress( $"http://localhost:{PeerPort}" );
    var client = new Messenger.MessengerClient( channel );

    Console.WriteLine( "PeerA started. Type messages to send to PeerB." );

    while ( true ) {
      var message = Console.ReadLine();
      var reply = await client.SendMessageAsync( new MessageRequest { From = "PeerA", Text = message } );

      Console.WriteLine( "PeerB replied: " + reply.Status );
    }
  }

  static void StartServer( int port ) {
    var server = new Server {
      Services = { Messenger.BindService( new MessengerImpl( "PeerA" ) ) },
      Ports = { new ServerPort( "localhost", port, ServerCredentials.Insecure ) }
    };

    server.Start();
    Console.WriteLine( $"PeerA server listening on port {port}" );
  }

  sealed class MessengerImpl : Messenger.MessengerBase {
    private readonly string _peerName;

    public MessengerImpl( string peerName ) {
      _peerName = peerName;
    }

    public override Task<MessageReply> SendMessage( MessageRequest request, ServerCallContext context ) {
      Console.WriteLine( $"{_peerName} received from {request.From}: {request.Text}" );
      return Task.FromResult( new MessageReply { Status = "Got it!" } );
    }
  }
}