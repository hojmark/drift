using System.CommandLine;
using Drift.Cli.Presentation.Console;

namespace Drift.Cli.Commands.Common;

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