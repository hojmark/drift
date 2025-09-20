using System.CommandLine;

namespace Drift.Cli.Commands.Lint;

internal record LintParameters : DefaultParameters {
  internal LintParameters( ParseResult parseResult ) : base( parseResult ) {
  }
}