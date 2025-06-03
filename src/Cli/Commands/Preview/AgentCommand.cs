using System.CommandLine;

namespace Drift.Cli.Commands.Preview;

internal class AgentCommand : Command {
  internal AgentCommand() : base( "agent", "Manage the local Drift agent" ) {
    var runCmd = new Command( "run", "Start the agent process" );
    runCmd.AddOption( new Option<bool>( ["--adoptable"],
      "Allow this agent to be adopted by another peer in the distributed agent network" ) );
    // terminology: agent network or agent group?
    // support @ for supplying local file
    runCmd.AddOption( new Option<string>( ["--join"], "Join the distributed agent network using a JWT" ) );
    runCmd.AddOption( new Option<bool>( ["--daemon", "-d"], "Run the agent as a background daemon" ) );
    runCmd.AddOption( new Option<bool>( ["--adoptable"],
      "Allow this agent to be adopted by another peer in the distributed agent network" ) );
    AddCommand( runCmd );

    // Support other init systems in the future
    var installCmd = new Command( "install", "Create agent systemd service file" );
    installCmd.AddOption( new Option<string>( ["--join"], "Join the distributed agent network using a JWT" ) );
    AddCommand( installCmd );

    var uninstallCmd = new Command( "uninstall", "Remove agent systemd service file" );
    AddCommand( uninstallCmd );

    var statusCmd = new Command( "status", "Show agent status" );
    AddCommand( statusCmd );

    //logs?
  }
}