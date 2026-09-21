namespace Drift.Scanning.Subnets;

// TODO needed?
public class CompositeSubnetProvider( IEnumerable<ISubnetProvider> providers ) : ISubnetProvider {
  private readonly List<ISubnetProvider> _providers = providers.ToList();

  public async Task<List<ResolvedSubnet>> GetAsync() {
    var results = await Task.WhenAll( _providers.Select( p => p.GetAsync() ) );
    return results.SelectMany( x => x ).Distinct().ToList();
  }
}