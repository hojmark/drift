using Drift.Domain;

namespace Drift.Scanning.Subnets;

public interface ISubnetProvider {
  Task<List<CidrBlock>> GetAsync();
}