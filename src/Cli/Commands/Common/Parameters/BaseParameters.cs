using System.CommandLine;
using Drift.Cli.Presentation.Console;

namespace Drift.Cli.Commands.Common.Parameters;

internal abstract record BaseParameters {
  protected BaseParameters( ParseResult parseResult ) {
    OutputFormat = parseResult.GetValue<OutputFormat>( CommonParameters.Options.OutputFormatName );
  }

  internal OutputFormat OutputFormat {
    get;
  }
}