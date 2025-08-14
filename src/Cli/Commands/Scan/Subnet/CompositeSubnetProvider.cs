using Drift.Domain;

namespace Drift.Cli.Commands.Scan.Subnet;

internal class CompositeSubnetProvider( IEnumerable<ISubnetProvider> providers ) : ISubnetProvider {
  private readonly List<ISubnetProvider> _providers = providers.ToList();

  public List<CidrBlock> Get() {
    return _providers.SelectMany( p => p.Get() ).Distinct().ToList();
  }
}