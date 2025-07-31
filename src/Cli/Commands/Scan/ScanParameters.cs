using System.CommandLine;

namespace Drift.Cli.Commands.Scan;

internal record ScanParameters : DefaultParameters {
  internal ScanParameters( ParseResult parseResult ) : base( parseResult ) {
  }
}