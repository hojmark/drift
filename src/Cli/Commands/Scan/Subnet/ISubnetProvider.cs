using Drift.Domain;

namespace Drift.Cli.Commands.Scan.Subnet;

internal interface ISubnetProvider {
  List<CidrBlock> Get();
}