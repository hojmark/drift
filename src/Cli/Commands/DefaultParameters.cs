using System.CommandLine;
using Drift.Cli.Commands.Common;
using Drift.Cli.Output;

namespace Drift.Cli.Commands;

internal record DefaultParameters {
  internal DefaultParameters( ParseResult parseResult ) {
    OutputFormat = parseResult.GetValue( CommonParameters.Options.OutputFormat );
    SpecFile = parseResult.GetValue( CommonParameters.Arguments.Spec );
  }

  internal OutputFormat OutputFormat {
    get;
  }

  internal FileInfo? SpecFile {
    get;
  }
}