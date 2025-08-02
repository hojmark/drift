using System.CommandLine;

namespace Drift.Cli.Commands.Scan;

public record ScanParameters : DefaultParameters {
  internal ScanParameters( ParseResult parseResult ) : base( parseResult ) {
  }
}