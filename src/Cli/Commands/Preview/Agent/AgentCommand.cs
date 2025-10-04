using System.CommandLine;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Lint;
using Drift.Cli.Presentation.Console.Managers.Abstractions;

namespace Drift.Cli.Commands.Preview.Agent;

internal class AgentCommand : CommandBase<AgentParameters, AgentCommandHandler> {
  internal AgentCommand( IServiceProvider provider ) : base( "agent", "Manage the local Drift agent", provider ) {
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

    var uninstallCmd = new Command( "uninstall", "Remove agent systemd service file" );
    Subcommands.Add( uninstallCmd );

    var statusCmd = new Command( "status", "Show agent status" );
    Subcommands.Add( statusCmd );*/
  }

  protected override AgentParameters CreateParameters( ParseResult result ) {
    return new AgentParameters( result );
  }
}

internal class AgentCommandHandler( IOutputManager output ) : ICommandHandler<AgentParameters> {
  public Task<int> Invoke( AgentParameters parameters, CancellationToken cancellationToken ) {
    throw new NotImplementedException();
  }
}