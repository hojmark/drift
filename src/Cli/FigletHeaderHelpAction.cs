using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace Drift.Cli;

internal class FigletHeaderHelpAction( HelpAction action ) : SynchronousCommandLineAction {
  public override int Invoke( ParseResult parseResult ) {
    AnsiConsole.Write(
      new FigletText( FigletFont.Load( EmbeddedResourceProvider.GetStream( "small.flf" ) ), "Drift" )
    );

    int result = action.Invoke( parseResult );

    // TODO specific 
    // Console.WriteLine( "USAGE: drift <command> [options]" );

    return result;
  }
}