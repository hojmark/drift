using System.CommandLine;
using Drift.Cli.Commands.Common;

namespace Drift.Cli.Commands;

internal record DefaultParameters {
  internal DefaultParameters( ParseResult parseResult ) {
    OutputFormat = parseResult.GetValue( CommonParameters.Options.OutputFormatOption );
    SpecFile = parseResult.GetValue( CommonParameters.Arguments.SpecOptional );
  }

  internal OutputFormat OutputFormat {
    get;
  }

  internal FileInfo? SpecFile {
    get;
  }
}