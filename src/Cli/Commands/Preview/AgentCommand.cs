using System.CommandLine;

namespace Drift.Cli.Commands.Preview;

internal class AgentCommand : Command {
  internal AgentCommand() : base( "agent", "Manage the local Drift agent" ) {
    var runCmd = new Command( "run", "Start the agent process" );
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
    Subcommands.Add( statusCmd );

    // logs?
  }
}