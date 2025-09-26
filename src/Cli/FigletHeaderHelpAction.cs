using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using Drift.Cli.Output;
using Spectre.Console;

namespace Drift.Cli;

internal class FigletHeaderHelpAction( HelpAction action ) : SynchronousCommandLineAction {
  public override int Invoke( ParseResult parseResult ) {
    var consoleOut = parseResult.Configuration.Output;
    var consoleErr = parseResult.Configuration.Error;

    var outputManager =
      new OutputManagerFactory().Create( OutputFormat.Normal, false, false, false, consoleOut, consoleErr, false );

    outputManager.Normal.GetAnsiConsole().Write(
      new FigletText( FigletFont.Load( EmbeddedResourceProvider.GetStream( "small.flf" ) ), "Drift" )
    );

    int result = action.Invoke( parseResult );

    // TODO specific 
    // Console.WriteLine( "USAGE: drift <command> [options]" );

    return result;
  }
}