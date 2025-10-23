using System.CommandLine;
using Drift.Cli.Commands.Agent.Subcommands;
using Drift.Cli.Commands.Common;
using Drift.Cli.Presentation.Console.Managers.Abstractions;

namespace Drift.Cli.Commands.Agent;

internal class AgentCommand : CommandBase<AgentParameters, AgentCommandHandler> {
  internal AgentCommand( IServiceProvider provider ) : base( "agent", "Manage the local Drift agent", provider ) {
    Subcommands.Add( new AgentStartCommand( provider ) );
    // Subcommands.Add( new AgentServiceCommand( provider ) );

    /*// Support other init systems in the future
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