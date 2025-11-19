using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console;
using Drift.Common.EmbeddedResources;
using Spectre.Console;

namespace Drift.Cli.Commands.Help;

internal class FigletHeaderHelpAction( HelpAction action ) : SynchronousCommandLineAction {
  public override int Invoke( ParseResult parseResult ) {
    var consoleOut = parseResult.InvocationConfiguration.Output;
    var consoleErr = parseResult.InvocationConfiguration.Error;

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