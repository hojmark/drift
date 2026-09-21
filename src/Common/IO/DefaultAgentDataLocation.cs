namespace Drift.Common.IO;

public sealed class DefaultAgentDataLocation( IDriftDataLocation driftDataLocation ) : IAgentDataLocation {
  public string Directory => Path.Combine( driftDataLocation.Directory, "agent" );
}