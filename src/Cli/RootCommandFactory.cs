using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using Drift.Cli.Commands.Init;
using Drift.Cli.Commands.Lint;
using Drift.Cli.Commands.Scan;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console;

namespace Drift.Cli;

internal static class RootCommandFactory {
  internal static RootCommand Create() {
    var loggerConfig = new LoggerConfiguration()
      .MinimumLevel.Debug()
      .Enrich.FromLogContext()
      .WriteTo.Console();

    Log.Logger = loggerConfig
      .CreateLogger();

    var loggerFactory = LoggerFactory.Create( builder => builder.AddSerilog()
        .SetMinimumLevel( LogLevel.Debug ) // Parse from args?
      /*.AddSimpleConsole( config => {
        config.SingleLine = true;
        config.TimestampFormat = "[HH:mm:ss.ffff] ";
      } )*/
    );

    //TODO 'from' or 'against'?
    var rootCommand = new RootCommand( "📡\uFE0F Drift CLI — monitor network drift against your declared state" );

    rootCommand.Subcommands.Add( new InitCommand( loggerFactory ) );
    rootCommand.Subcommands.Add( new ScanCommand( loggerFactory ) );
    rootCommand.Subcommands.Add( new LintCommand( loggerFactory ) );

    for ( int i = 0; i < rootCommand.Options.Count; i++ ) {
      // RootCommand has a default HelpOption, we need to update its Action.
      if ( rootCommand.Options[i] is HelpOption defaultHelpOption ) {
        defaultHelpOption.Action = new CustomHelpAction( (HelpAction) defaultHelpOption.Action! );
        break;
      }
    }

    return rootCommand;
  }
}

internal class CustomHelpAction : SynchronousCommandLineAction {
  private readonly HelpAction _defaultHelp;

  public CustomHelpAction( HelpAction action ) => _defaultHelp = action;

  public override int Invoke( ParseResult parseResult ) {
    AnsiConsole.Write(
      new FigletText( FigletFont.Load( EmbeddedResourceProvider.GetStream( "small.flf" ) ), "Drift" ) );

    int result = _defaultHelp.Invoke( parseResult );

    return result;
  }
}