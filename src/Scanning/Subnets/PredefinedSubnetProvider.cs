using Drift.Domain;

namespace Drift.Scanning.Subnets;

public class PredefinedSubnetProvider( IEnumerable<DeclaredSubnet> subnets ) : ISubnetProvider {
  public List<CidrBlock> Get() {
    return subnets
      .Where( s => s.Enabled ?? true )
      .Select( s => new CidrBlock( s.Address ) )
      .ToList();
  }
}