using System.CommandLine;

namespace Drift.Cli.Commands.Preview;

internal class LintCommand : Command {
  private LintCommand() : base( "lint", "Validate spec and env files" ) {
    var strict = new Option<bool>( ["--strict"], "Strict mode" );
    AddOption( strict );
  }
}