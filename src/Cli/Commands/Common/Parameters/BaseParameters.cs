using System.CommandLine;
using Drift.Cli.Presentation.Console;

namespace Drift.Cli.Commands.Common.Parameters;

internal abstract record BaseParameters {
  protected BaseParameters( ParseResult parseResult ) {
    OutputFormat = parseResult.GetValue( CommonParameters.Options.OutputFormat );
  }

  internal OutputFormat OutputFormat {
    get;
  }
}