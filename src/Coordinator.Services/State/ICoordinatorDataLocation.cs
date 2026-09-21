namespace Drift.Coordinator.Services.State;

/// <summary>
/// Defines the coordinator-owned files and directories that make up its local data root.
/// </summary>
public interface ICoordinatorDataLocation {
  string Directory {
    get;
  }

  string SpecFile {
    get;
  }

  string AgentEnrollmentFile {
    get;
  }

  string ScansDirectory {
    get;
  }

  void EnsureCreated();
}