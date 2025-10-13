using System;
using Drift.Build.Utilities.MsBuild;
using Nuke.Common;
using Serilog;
using Utilities;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

sealed partial class NukeBuild {
  private const string BinaryBuildLogName = "build.binlog";
  private const string BinaryPublishLogName = "publish.binlog";

  Target CheckWarnings => _ => _
    .DependsOn( CheckBuildWarnings, CheckPublishBinariesWarnings );

  Target CheckBuildWarnings => _ => _
    .After( Build )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(CheckBuildWarnings) );

        var warnings = BinaryLogReader.GetWarnings( BinaryBuildLogName );

        foreach ( var warning in warnings ) {
          Log.Information( warning );
        }

        var hasWarnings = warnings.Length != 0;

        if ( hasWarnings ) {
          Log.Error( "Found {Count} build warnings", warnings.Length );
          throw new Exception( $"Found {warnings.Length} build warnings" );
        }

        Log.Information( "🟢 No build warnings found" );
      }
    );

  Target CheckPublishBinariesWarnings => _ => _
    .After( CheckBuildWarnings, PublishBinaries )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(CheckPublishBinariesWarnings) );

        var warnings = BinaryLogReader.GetWarnings( BinaryPublishLogName );

        foreach ( var warning in warnings ) {
          Log.Information( warning );
        }

        var hasWarnings = warnings.Length != 0;

        if ( hasWarnings ) {
          Log.Error( "Found {Count} binary publish warnings", warnings.Length );
          throw new Exception( $"Found {warnings.Length} binary publish warnings" );
        }

        Log.Information( "🟢 No binary publish warnings found" );
      }
    );
}