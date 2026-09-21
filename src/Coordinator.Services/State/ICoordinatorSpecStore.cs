using Drift.Domain;

namespace Drift.Coordinator.Services.State;

public interface ICoordinatorSpecStore {
  string LoadYaml();

  Inventory Load();

  Task ReplaceAsync( string yaml, CancellationToken cancellationToken );
}