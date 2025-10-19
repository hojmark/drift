using System.CommandLine;
using Drift.Cli.Presentation.Console;

namespace Drift.Cli.Commands.Common;

internal record DefaultParameters : BaseParameters {
  internal DefaultParameters( ParseResult parseResult ) : base( parseResult ) {
    SpecFile = parseResult.GetValue( CommonParameters.Arguments.Spec );
  }

  internal FileInfo? SpecFile {
    get;
  }
}