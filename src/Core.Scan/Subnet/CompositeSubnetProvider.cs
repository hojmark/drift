using Drift.Domain;

namespace Drift.Core.Scan.Subnet;

//TODO needed?
public class CompositeSubnetProvider( IEnumerable<ISubnetProvider> providers ) : ISubnetProvider {
  private readonly List<ISubnetProvider> _providers = providers.ToList();

  public List<CidrBlock> Get() {
    return _providers.SelectMany( p => p.Get() ).Distinct().ToList();
  }
}