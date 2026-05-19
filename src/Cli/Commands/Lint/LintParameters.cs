using System.CommandLine;
using Drift.Cli.Commands.Common.Parameters;

namespace Drift.Cli.Commands.Lint;

internal record LintParameters : SpecParameters {
  internal LintParameters( ParseResult parseResult ) : base( parseResult ) {
  }
}