using System.CommandLine;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Common.Parameters;

namespace Drift.Cli.Commands.Scan;

internal record ScanParameters : SpecParameters {
  internal static class Options {
    internal static readonly Option<bool> Interactive = new("--interactive", "-i") {
      Description = "Interactive mode", Arity = ArgumentArity.Zero
    };
  }

  internal ScanParameters( ParseResult parseResult ) : base( parseResult ) {
    Interactive = parseResult.GetValue( Options.Interactive );
    ShowLogPanel = parseResult.GetValue( CommonParameters.Options.Verbose ) ||
                      parseResult.GetValue( CommonParameters.Options.VeryVerbose );
  }

  internal bool Interactive {
    get;
  }

  internal bool ShowLogPanel {
    get;
  }
}