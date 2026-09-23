using Drift.Domain;

namespace Drift.Scanning.Subnets;

public class PredefinedSubnetProvider( IEnumerable<DeclaredSubnet> subnets ) : ISubnetProvider {
  public Task<List<ResolvedSubnet>> GetAsync() {
    return Task.FromResult(
      subnets
        .Where( s => s.Enabled ?? true )
        .Select( s => new ResolvedSubnet(
          new CidrBlock( s.Address ),
          // TODO how to determine source when from spec?
          SubnetSource.Local
        ) )
        .ToList()
    );
  }
}