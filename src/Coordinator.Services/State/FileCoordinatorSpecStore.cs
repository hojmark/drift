using Drift.Domain;
using Drift.Spec.Serialization;

namespace Drift.Coordinator.Services.State;

internal sealed class FileCoordinatorSpecStore( ICoordinatorDataLocation dataLocation ) : ICoordinatorSpecStore {
  public string LoadYaml() {
    return File.Exists( dataLocation.SpecFile )
      ? File.ReadAllText( dataLocation.SpecFile )
      : string.Empty;
  }

  public Inventory Load() {
    var yaml = LoadYaml();
    return string.IsNullOrWhiteSpace( yaml )
      ? new Inventory { Network = new() }
      : YamlConverter.Deserialize( yaml );
  }

  public async Task ReplaceAsync( string yaml, CancellationToken cancellationToken ) {
    dataLocation.EnsureCreated();
    var temporaryFile = dataLocation.SpecFile + ".tmp";
    await File.WriteAllTextAsync( temporaryFile, yaml, cancellationToken );
    File.Move( temporaryFile, dataLocation.SpecFile, true );
  }
}