using Drift.Cli.Commands.Common;
using Drift.Domain;

namespace Drift.Cli.Tests.Utils;

public class PredefinedSpecProvider( Inventory inventory ) : ISpecProvider {
  public Task<(FileInfo? Path, string Contents)?> GetAsync( string? name ) {
    throw new NotImplementedException();
  }

  public Task<Inventory> DeserializeAsync( string? name ) {
    return Task.FromResult( inventory );
  }
}