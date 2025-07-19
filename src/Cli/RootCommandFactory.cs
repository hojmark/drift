using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using Drift.Cli.Commands.Init;
using Drift.Cli.Commands.Lint;
using Drift.Cli.Commands.Scan;
using Drift.Cli.Output;
using Spectre.Console;

namespace Drift.Cli;

internal static class RootCommandFactory {
  internal static RootCommand Create( bool toConsole ) {
    var outputManagerFactory = new OutputManagerFactory( toConsole );

    //TODO 'from' or 'against'?
    var rootCommand = new RootCommand( "📡\uFE0F Drift CLI — monitor network drift against your declared state" );

    rootCommand.Subcommands.Add( new InitCommand( outputManagerFactory ) );
    rootCommand.Subcommands.Add( new ScanCommand( outputManagerFactory ) );
    rootCommand.Subcommands.Add( new LintCommand( outputManagerFactory ) );
    rootCommand.TreatUnmatchedTokensAsErrors = true;

    foreach ( var t in rootCommand.Options ) {
      // Update the default HelpOption of the RootCommand
      if ( t is HelpOption defaultHelpOption ) {
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

    // TODO specific 
    // Console.WriteLine( "USAGE: drift <command> [options]" );

    return result;
  }
}