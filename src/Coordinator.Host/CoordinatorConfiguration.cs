namespace Drift.Coordinator.Host;

public class CoordinatorConfiguration {
  public ushort Port {
    get;
    init;
  }

  public ushort? AgentPort {
    get;
    init;
  }

  public bool EnableRequestLogging {
    get;
    init;
  } = true;
}