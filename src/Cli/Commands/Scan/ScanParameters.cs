using System.CommandLine;

namespace Drift.Cli.Commands.Scan;

public record ScanParameters : DefaultParameters {
  internal static class Options {
    internal static readonly Option<bool> Interactive = new("--interactive", "-i") {
      Description = "Interactive mode", Arity = ArgumentArity.Zero, Hidden = true
    };
  }

  internal ScanParameters( ParseResult parseResult ) : base( parseResult ) {
    Interactive = parseResult.GetValue( Options.Interactive );
  }

  internal bool Interactive {
    get;
  }
}