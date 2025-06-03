using Drift.Domain;

namespace Drift.Cli.Commands.Scan.Subnet;

internal class PrioritizedSubnetProvider( IEnumerable<ISubnetProvider> providers ) : ISubnetProvider {
  private readonly List<ISubnetProvider> _providers = providers.ToList();

  public List<CidrBlock> Get() {
    foreach ( var provider in _providers ) {
      var subnets = provider.Get();
      if ( subnets.Any() )
        return subnets;
    }

    return [];
  }
}