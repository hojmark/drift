using System.CommandLine;
using System.CommandLine.Help;

namespace Drift.Cli;

internal class CustomHelpBuilder() : HelpBuilder( LocalizationResources.Instance ) {
  public override void Write( HelpContext context ) {
    var command = context.Command;
    var descLines = command.Description?.Split( "\n\n", 2 );

    if ( descLines?.Length > 0 ) {
      command.Description = descLines[0]; // only the summary goes to default formatter
    }

    base.Write( context );

    if ( descLines?.Length == 2 ) {
      // Justification: prototype
#pragma warning disable RS0030
      Console.WriteLine( descLines[1].Trim() ); // print the examples section manually
#pragma warning restore RS0030
    }
  }
}