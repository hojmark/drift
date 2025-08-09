using Drift.Cli.Commands.Common;
using Drift.Domain;

namespace Drift.Cli.Tests.Utils;

public class PredefinedSpecProvider( Inventory inventory ) : ISpecFileProvider {
  public Task<Inventory?> GetDeserializedAsync( FileInfo? specFile ) {
    return Task.FromResult( inventory );
  }
}