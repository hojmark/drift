using Drift.Domain;

namespace Drift.Core.Scan.Subnet;

public class DeclaredSubnetProvider( IEnumerable<DeclaredSubnet> subnets ) : ISubnetProvider {
  public List<CidrBlock> Get() {
    return subnets
      .Where( s => s.Enabled ?? true )
      .Select( s => new CidrBlock( s.Address ) )
      .ToList();
  }
}