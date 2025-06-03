using Drift.Domain;

namespace Drift.Cli.Commands.Scan.Subnet;

internal class DeclaredSubnetProvider( IEnumerable<DeclaredSubnet> subnets ) : ISubnetProvider {
  public List<CidrBlock> Get() {
    return subnets
      .Where( s => s.Enabled ?? true )
      .Select( s => new CidrBlock( s.Network ) )
      .ToList();
  }
}