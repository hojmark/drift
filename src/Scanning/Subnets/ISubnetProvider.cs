using Drift.Domain;

namespace Drift.Scanning.Subnets;

public interface ISubnetProvider {
  List<CidrBlock> Get();
}