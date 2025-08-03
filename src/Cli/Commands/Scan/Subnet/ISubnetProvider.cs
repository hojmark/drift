using Drift.Domain;

namespace Drift.Cli.Commands.Scan.Subnet;

public interface ISubnetProvider {
  List<CidrBlock> Get();
}