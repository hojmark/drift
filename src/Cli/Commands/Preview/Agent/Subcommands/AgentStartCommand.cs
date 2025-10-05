using System.Collections.Concurrent;
using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.SpecFile;
using Drift.Domain;
using Drift.Networking.Grpc.Generated;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using PeerService = Drift.Cli.Commands.Preview.Agent.Subcommands.Utils.PeerService;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands;

internal class AgentStartCommand : CommandBase<AgentStartParameters, AgentStartCommandHandler> {
  internal AgentStartCommand( IServiceProvider provider ) : base( "start", "Manage the local Drift agent", provider ) {
    /*var runCmd = new Command( "start", "Start the agent process" );
    runCmd.Options.Add( new Option<bool>( "--adoptable" ) {
      Description = "Allow this agent to be adopted by another peer in the distributed agent network"
    } );
    // terminology: agent network or agent group?
    // support @ for supplying local file
    runCmd.Options.Add( new Option<string>( "--join" ) {
      Description = "Join the distributed agent network using a JWT"
    } );
    runCmd.Options.Add( new Option<bool>( "--daemon", "-d" ) { Description = "Run the agent as a background daemon" } );
    runCmd.Options.Add( new Option<bool>( "--adoptable"
    ) { Description = "Allow this agent to be adopted by another peer in the distributed agent network" } );
    Subcommands.Add( runCmd );

    // Support other init systems in the future
    var installCmd = new Command( "install", "Create agent systemd service file" );
    installCmd.Options.Add( new Option<string>( "--join" ) {
      Description = "Join the distributed agent network using a JWT"
    } );
    Subcommands.Add( installCmd );

    // or: drift agent service install
    var uninstallCmd = new Command( "uninstall", "Remove agent systemd service file" );
    Subcommands.Add( uninstallCmd );

    var statusCmd = new Command( "status", "Show agent status" );
    Subcommands.Add( statusCmd );*/
  }

  protected override AgentStartParameters CreateParameters( ParseResult result ) {
    return new AgentStartParameters( result );
  }
}

internal class AgentStartCommandHandler(
  IOutputManager output,
  ISpecFileProvider specProvider
) : ICommandHandler<AgentStartParameters> {
  public async Task<int> Invoke( AgentStartParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'agent start' command" );

    Inventory? inventory;

    try {
      inventory = await specProvider.GetDeserializedAsync( parameters.SpecFile );
    }
    catch ( FileNotFoundException ) {
      return ExitCodes.GeneralError;
    }

    output.Log.LogDebug( "Starting agent..." );

    var builder = WebApplication.CreateBuilder();

    builder.Logging.ClearProviders();
    builder.Logging.AddProvider( new PredefinedLoggerProvider( output.Log ) );

    builder.Services.AddGrpc();

    builder.WebHost.ConfigureKestrel( options => {
      options.ListenLocalhost( 51515, o => {
        o.Protocols = HttpProtocols.Http2; // Allow HTTP/2 over plain HTTP
      } );
    } );

    var app = builder.Build();

    app.MapGrpcService<PeerService>();
    // app.MapGrpcReflectionService();
    app.MapGet( "/", () => "Nothing to see here" );

    app.Lifetime.ApplicationStarted.Register( () => {
      var clientStreams = new ConcurrentDictionary<string, AsyncDuplexStreamingCall<PeerMessage, PeerMessage>>();
      foreach ( var agent in inventory.Agents ) {
        var address = agent.Address;
        var channel = GrpcChannel.ForAddress( address );
        var client = new Drift.Networking.Grpc.Generated.PeerService.PeerServiceClient( channel );

        var call = client.PeerStream();

        // Handle incoming messages from all peers
        _ = Task.Run( async () => {
          await foreach ( var msg in call.ResponseStream.ReadAllAsync() ) {
            Console.WriteLine( $"Received from {msg.PeerId}@{address}: {msg.Message}" );
          }
        } );

        clientStreams[address] = call;
      }
    } );

    await app.RunAsync();

    output.Log.LogDebug( "Completed 'agent start' command" );

    return ExitCodes.Success;
  }
}