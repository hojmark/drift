using System.CommandLine;
using Drift.Cli.Commands.Common;

namespace Drift.Cli.Commands.Lint;

internal record LintParameters : DefaultParameters {
  internal LintParameters( ParseResult parseResult ) : base( parseResult ) {
  }
}