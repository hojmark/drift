using System.CommandLine;
using Drift.Cli.Commands.Preview.Config;

namespace Drift.Cli.Commands.Preview;

// Settings location: ~/.config/drift/settings.json
/*
   emojis: false
   color: true
   telemetry: false
   defaultEnv: main-site.env.yaml
   defaultOption: y,n,recommended
 */
// consider 'git config --help'
internal class ConfigCommand : Command {
  internal ConfigCommand() : base( "config", "View or change user preferences" ) {
    var showCommand = new Command( "list", "Display all current user settings" );
    showCommand.SetAction( _ => {
      // var settings = UserSettings.Load();
      // Console.WriteLine(YamlSerializer.Serialize(settings));
    } );
    Subcommands.Add( showCommand );

    var getCommand = new Command( "get", "Get a specific setting" );
    getCommand.Arguments.Add( new Argument<string>( "key" ) );
    /*  getCommand.SetHandler( ( string key ) => {
        var settings = UserSettings.Load();
        var value = key.ToLower() switch {
          "emojis" => settings.Emojis.ToString(),
          "color" => settings.Color.ToString(),
          "telemetry" => settings.Telemetry.ToString(),
          "defaultenv" => settings.DefaultEnv ?? "(none)",
          "defaultspec" => settings.DefaultSpec ?? "(none)",
          _ => "Unknown setting"
        };
        Console.WriteLine( value );
      } );*/
    Subcommands.Add( getCommand );

    var setCommand = new Command( "set", "Set a specific setting" );
    setCommand.Arguments.Add( new Argument<string>( "key" ) );
    setCommand.Arguments.Add( new Argument<string>( "value" ) );
    /* setCommand.SetHandler( ( string key, string value ) => {
       var settings = UserSettings.Load();
       switch ( key.ToLower() ) {
         case "emojis": settings.Emojis = bool.Parse( value ); break;
         case "color": settings.Color = bool.Parse( value ); break;
         case "telemetry": settings.Telemetry = bool.Parse( value ); break;
         case "defaultenv": settings.DefaultEnv = value; break;
         case "defaultspec": settings.DefaultSpec = value; break;
         default:
           Console.WriteLine( "❌ Unknown setting." );
           return;
       }

       settings.Save();
       Console.WriteLine( "✅ Setting updated." );
     } );*/
    Subcommands.Add( setCommand );

    var resetCommand = new Command( "reset", "Clear all user settings" );
    resetCommand.SetAction( _ => {
      UserSettings.Reset();
      // Console.WriteLine( "✅ Settings cleared." );
    } );
    Subcommands.Add( resetCommand );
  }
}