using Drift.Domain;

namespace Drift.Scanning.Subnets;

public abstract record SubnetSource {
  public static readonly Local Local = new();

  public static Agent Agent( AgentId agentId ) {
    ArgumentNullException.ThrowIfNull( agentId );
    return new Agent( agentId );
  }
}

public sealed record Agent( AgentId AgentId ) : SubnetSource {
  public override string ToString() {
    return AgentId;
  }
}

public sealed record Local : SubnetSource {
  public override string ToString() {
    return "local";
  }
}

public sealed record ResolvedSubnet( CidrBlock Cidr, SubnetSource Source );