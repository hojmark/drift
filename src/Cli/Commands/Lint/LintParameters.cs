using System.CommandLine;

namespace Drift.Cli.Commands.Lint;

public record LintParameters : DefaultParameters {
  internal LintParameters( ParseResult parseResult ) : base( parseResult ) {
  }
}