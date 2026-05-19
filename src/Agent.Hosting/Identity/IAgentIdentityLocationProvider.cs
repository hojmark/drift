namespace Drift.Agent.Hosting.Identity;

public interface IAgentIdentityLocationProvider {
  string GetDirectory();

  string GetFile() {
    return Path.Combine( GetDirectory(), "agent-identity.json" );
  }
}
