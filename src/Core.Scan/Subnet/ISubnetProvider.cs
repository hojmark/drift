using Drift.Domain;

namespace Drift.Core.Scan.Subnet;

public interface ISubnetProvider {
  List<CidrBlock> Get();
}