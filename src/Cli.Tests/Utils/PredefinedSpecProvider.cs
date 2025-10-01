using Drift.Cli.SpecFile;
using Drift.Domain;

namespace Drift.Cli.Tests.Utils;

internal sealed class PredefinedSpecProvider( Dictionary<string, Inventory> inventories ) : ISpecFileProvider {
  public Task<Inventory?> GetDeserializedAsync( FileInfo? specFile ) {
    return specFile == null
      ? Task.FromResult<Inventory?>( null )
      : Task.FromResult( inventories.GetValueOrDefault( specFile.Name ) );
  }
}