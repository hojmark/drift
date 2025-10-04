using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

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

internal class AgentStartCommandHandler( IOutputManager output ) : ICommandHandler<AgentStartParameters> {
  public async Task<int> Invoke( AgentStartParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'agent start' command" );

    output.Log.LogDebug( "Starting agent..." );

    var builder = WebApplication.CreateBuilder();

    builder.Logging.ClearProviders();
    builder.Logging.AddProvider( new PredefinedLoggerProvider( output.Log ) );

    builder.Services.AddGrpc();

    builder.WebHost.ConfigureKestrel( options => {
      options.ListenLocalhost( 5000, o => {
        o.Protocols = HttpProtocols.Http2; // Allow HTTP/2 over plain HTTP
      } );
    } );

    var app = builder.Build();

    app.MapGrpcService<ChatService>();
    //app.MapGrpcReflectionService();
    app.MapGet( "/", () => "Hello, world!" );

    await app.RunAsync();

    output.Log.LogDebug( "Completed 'agent start' command" );

    return ExitCodes.Success;
  }
}