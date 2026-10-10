using Drift.Common.IO;

namespace Drift.Coordinator.Services.State;

/// <summary>
/// Resolves the coordinator data location within Drift's data directory.
/// </summary>
internal sealed class DefaultCoordinatorDataLocation( IDriftDataLocation driftDataLocation )
  : ICoordinatorDataLocation {
  public string Directory => Path.Combine( driftDataLocation.Directory, "coordinator" );

  public string ScansDirectory => Path.Combine( Directory, "scans" );

  public string SpecFile => Path.Combine( Directory, "spec.yaml" );

  public string AgentEnrollmentFile => Path.Combine( Directory, "agent-enrollment.json" );

  /// <summary>
  /// Creates the coordinator data location and its scan-results directory if they do not already exist.
  /// </summary>
  public void EnsureCreated() {
    System.IO.Directory.CreateDirectory( Directory );
    System.IO.Directory.CreateDirectory( ScansDirectory );
  }
}