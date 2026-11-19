using Drift.Common.IO;

namespace Drift.Agent.Host.State;

/// <summary>
/// Resolves the agent data location within Drift's data directory.
/// </summary>
public sealed class DefaultAgentDataLocation( IDriftDataLocation driftDataLocation ) : IAgentDataLocation {
  public string Directory => Path.Combine( driftDataLocation.Directory, "agent" );

  public void EnsureCreated() {
    System.IO.Directory.CreateDirectory( Directory );
  }
}