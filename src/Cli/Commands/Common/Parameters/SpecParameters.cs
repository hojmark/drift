using System.CommandLine;

namespace Drift.Cli.Commands.Common.Parameters;

internal abstract record SpecParameters : BaseParameters {
  protected SpecParameters( ParseResult parseResult ) : base( parseResult ) {
    SpecFile = parseResult.GetValue( CommonParameters.Arguments.Spec );
  }

  internal FileInfo? SpecFile {
    get;
  }
}