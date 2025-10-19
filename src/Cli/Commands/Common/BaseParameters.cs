using System.CommandLine;
using Drift.Cli.Presentation.Console;

namespace Drift.Cli.Commands.Common;

internal record BaseParameters {
  internal BaseParameters( ParseResult parseResult ) {
    OutputFormat = parseResult.GetValue( CommonParameters.Options.OutputFormat );
  }

  internal OutputFormat OutputFormat {
    get;
  }
}