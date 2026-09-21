using Drift.Coordinator.Services.State;

namespace Drift.Cli.Tests.Utils.Coordinator;

// TODO duplicate type
internal sealed class TemporaryCoordinatorDataLocation : ICoordinatorDataLocation {
  private readonly string _directory = System.IO.Directory.CreateTempSubdirectory( "drift-coordinator-test-" ).FullName;

  public string Directory => _directory;

  public string SpecFile => Path.Combine( Directory, "spec.yaml" );

  public string AgentEnrollmentFile => Path.Combine( Directory, "agent-enrollment.json" );

  public string ScansDirectory => Path.Combine( Directory, "scans" );

  public void EnsureCreated() {
    System.IO.Directory.CreateDirectory( Directory );
    System.IO.Directory.CreateDirectory( ScansDirectory );
  }
}