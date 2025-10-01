using System.CommandLine;

namespace Drift.Cli.Commands.Preview.Config;

internal class ConfigCommand : Command {
  internal ConfigCommand() : base( "config", "Configure settings for the application" ) {
    var setCommand = new Command( "set", "Set a configuration value" ) {
      new Argument<string>( "key" ) { Description = "The config key to set" },
      new Argument<string>( "value" ) { Description = "The value to assign" }
    };
    setCommand.SetAction( _ => {
      //Console.WriteLine( $"Setting {key} to {value}" );
    } );

    var getCommand =
      new Command( "get", "Get a configuration value" ) {
        new Argument<string>( "key" ) { Description = "The config key to get" }
      };
    getCommand.SetAction( _ => {
      //Console.WriteLine( $"Getting value for {key}" );});
    } );

    var listCommand = new Command( "list", "List all configuration values" );
    listCommand.SetAction( _ => {
      //Console.WriteLine( "Listing all config values..." );
    } );

    var unsetCommand =
      new Command( "unset", "Unset a configuration value" ) {
        new Argument<string>( "key" ) { Description = "The config key to unset" }
      };
    unsetCommand.SetAction( _ => {
      //Console.WriteLine( $"Unsetting value for {key}" );
    } );

    Subcommands.Add( setCommand );
    Subcommands.Add( getCommand );
    Subcommands.Add( listCommand );
    Subcommands.Add( unsetCommand );
  }
}