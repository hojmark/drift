using Drift.Cli.Commands.Agent.Subcommands.Start;
using Drift.Cli.Commands.Common.Commands;

namespace Drift.Cli.Commands.Agent;

internal class AgentCommand : ContainerCommandBase {
  internal AgentCommand( IServiceProvider provider ) : base( "agent", "Manage the local Drift agent" ) {
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
}