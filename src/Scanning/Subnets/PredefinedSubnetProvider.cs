using Drift.Domain;

namespace Drift.Scanning.Subnets;

public class PredefinedSubnetProvider( IEnumerable<DeclaredSubnet> subnets ) : ISubnetProvider {
  public Task<List<CidrBlock>> GetAsync() {
    return Task.FromResult(
      subnets
        .Where( s => s.Enabled ?? true )
        .Select( s => new CidrBlock( s.Address ) )
        .ToList()
    );
  }
}