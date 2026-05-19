using System.CommandLine;
using Drift.Cli.Commands.Common.Parameters;

namespace Drift.Cli.Commands.Init;

internal record InitParameters : SpecParameters {
  internal bool? Discover {
    get;
  }

  internal bool? Overwrite {
    get;
  }

  internal ForceMode? ForceMode {
    get;
  }

  internal InitParameters( ParseResult result ) : base( result ) {
    Discover = result.GetValue( InitCommand.DiscoverOption );
    Overwrite = result.GetValue( InitCommand.OverwriteOption );
    ForceMode = result.GetValue( InitCommand.ForceModeOption );
  }
}