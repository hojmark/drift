using System.CommandLine;
using Drift.Domain;
using Drift.Parsers.EnvironmentJson;
using Spectre.Console;
using Environment = Drift.Domain.Environment;

namespace Drift.Cli.Commands.Preview;

/*
 * Environment/cluster: main-site [Active]
   Agent: Agent-01 [Running, Healthy]
   Agent: Agent-02 [Stopped, No Auth]
   Agent: Agent-03 [Running, Healthy]

   Other agents:
   Agent: Agent-04 [Running, Pending Adoption]
   Agent: Agent-04 [Running, Unknown]
 */
//TODO or ClusterCommand?
internal class EnvCommand : Command {
  internal EnvCommand() : base( "env", "Manage agent clusters environments" ) {
    var tokenCmd = new Command( "token", "Create a short-lived JWT that allows an agent to join an environment." );
    tokenCmd.AddArgument( new Argument<string>( "cluster-name", "The cluster name" ) );
    AddCommand( tokenCmd );
    
    var statusCmd = new Command( "status", "Real-time status of environments (heartbeat-like check)" );
    statusCmd.AddArgument( new Argument<string>( "cluster-name", "The cluster name" ) );
    AddCommand( statusCmd );

    var listEnvs = new Command( "list", "List environments." ); // env list my-agent-cluster
    listEnvs.AddArgument( new Argument<string>( "cluster-name", "The cluster name" ) );
    listEnvs.SetHandler( context => {
        var envConfigPath = "............ drift-env.json";
        var json = File.ReadAllText( envConfigPath );
        var environment = JsonConverter.Deserialize<Environment>( json );

        // Print
        var table = new Table().ShowRowSeparators();

        table.AddColumn( new TableColumn( nameof(Environment.Name) ).Centered() );
        table.AddColumn( new TableColumn( "Agents" ).Centered() );
        //table.AddColumn( new TableColumn( "" ).Centered() );

        table.AddRow( environment.Name + ( environment.Active ? "\u26a1 [green]Active[/]" : "" ),
          string.Join( "\n", environment.Agents.Select( a =>
              a.Address + " " + ( a.Authentication.Type == AuthType.None
                ? "[red]No auth[/]"
                : "\ud83d\udd12" )
            )
          )
        );

        AnsiConsole.Write( table );
        //Ui.WriteLine("config: "+ envConfigPath,ConsoleColor.DarkGray);
      }
    );
    AddCommand( listEnvs );
  }
}