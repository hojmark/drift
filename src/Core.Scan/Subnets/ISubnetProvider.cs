using Drift.Domain;

namespace Drift.Core.Scan.Subnets;

public interface ISubnetProvider {
  List<CidrBlock> Get();
}