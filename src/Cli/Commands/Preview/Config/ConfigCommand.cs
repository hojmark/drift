using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace Drift.Cli.Commands.Preview.Config;

internal class ConfigCommand : Command {
  internal ConfigCommand() : base( "config", "Configure settings for the application" ) {
    var setCommand = new Command( "set", "Set a configuration value" ) {
      new Argument<string>( "key", "The config key to set" ), new Argument<string>( "value", "The value to assign" )
    };
    setCommand.Handler = CommandHandler.Create<string, string>( ( key, value ) => {
      //Console.WriteLine( $"Setting {key} to {value}" );
    } );

    var getCommand =
      new Command( "get", "Get a configuration value" ) { new Argument<string>( "key", "The config key to get" ) };
    getCommand.Handler = CommandHandler.Create<string>( ( key ) => {
      //Console.WriteLine( $"Getting value for {key}" );
    } );

    var listCommand = new Command( "list", "List all configuration values" );
    listCommand.Handler = CommandHandler.Create( () => {
      //Console.WriteLine( "Listing all config values..." );
    } );

    var unsetCommand =
      new Command( "unset", "Unset a configuration value" ) {
        new Argument<string>( "key", "The config key to unset" )
      };
    listCommand.Handler = CommandHandler.Create<string>( ( key ) => {
      //Console.WriteLine( $"Unsetting value for {key}" );
    } );

    AddCommand( setCommand );
    AddCommand( getCommand );
    AddCommand( listCommand );
    AddCommand( unsetCommand );
  }
}