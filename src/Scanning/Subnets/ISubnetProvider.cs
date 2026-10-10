namespace Drift.Scanning.Subnets;

public interface ISubnetProvider {
  Task<List<ResolvedSubnet>> GetAsync();
}