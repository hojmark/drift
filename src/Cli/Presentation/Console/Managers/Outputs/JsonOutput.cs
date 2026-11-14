using Drift.Cli.Presentation.Console.Managers.Abstractions;

namespace Drift.Cli.Presentation.Console.Managers.Outputs;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
  "ApiDesign",
  "RS0030:Do not use banned APIs",
  Justification = "JSON output must write its serialized document directly to stdout."
)]
internal sealed class JsonOutput( TextWriter stdOut ) : IJsonOutput {
  public void WriteLine( string text ) {
    stdOut.WriteLine( text );
  }
}