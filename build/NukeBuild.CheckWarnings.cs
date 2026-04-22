using System;
using System.Linq;
using Drift.Build.Utilities;
using Drift.Build.Utilities.MsBuild;
using Nuke.Common;
using Serilog;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

sealed partial class NukeBuild {
  private const string BinaryBuildLogName = "build.binlog";
  private const string BinaryPublishLogName = "publish.binlog";

  /*
   * Ignore NuGet vulnerability warnings. The audit.yaml workflow will fail if any of these are detected.
   * NU1901: Package with low severity detected
   * NU1902: Package with moderate severity detected
   * NU1903: Package with high severity detected
   * NU1904: Package with critical severity detected
   */
  private static readonly string[] IgnoredBuildWarnings = ["NU1901", "NU1902", "NU1903", "NU1904"];

  Target CheckWarnings => _ => _
    .DependsOn( CheckBuildWarnings, CheckPublishBinariesWarnings );

  Target CheckBuildWarnings => _ => _
    .After( Build )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(CheckBuildWarnings) );

        var warnings = BinaryLogReader.GetWarnings( BinaryBuildLogName )
          .Select( w => new { Warning = w, Ignored = IgnoredBuildWarnings.Any( w.Contains ) } )
          .ToArray();

        foreach ( var w in warnings ) {
          if ( w.Ignored ) {
            Log.Debug( "{WarningMessage} (ignored)", w.Warning );
          }
          else {
            Log.Information( "{WarningMessage}", w.Warning );
          }
        }

        var activeWarnings = warnings.Where( w => !w.Ignored ).ToArray();

        if ( activeWarnings.Any() ) {
          Log.Error( "Found {Count} build warnings", activeWarnings.Length );
          throw new Exception( $"Found {activeWarnings.Length} build warnings" );
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