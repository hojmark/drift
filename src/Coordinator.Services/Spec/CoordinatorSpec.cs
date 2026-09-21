using Drift.Coordinator.Services.State;
using Drift.Domain;

namespace Drift.Coordinator.Services.Spec;

/// <summary>
/// Owns the coordinator's currently loaded inventory and its agent declarations.
/// </summary>
public sealed class CoordinatorSpec {
  private readonly Func<Inventory> _load;
  private Inventory _inventory;

  public CoordinatorSpec( ICoordinatorSpecStore specStore ) {
    _load = specStore.Load;
    _inventory = _load();
  }

  internal CoordinatorSpec( Inventory inventory ) {
    _load = () => inventory;
    _inventory = inventory;
  }

  /// <summary>
  /// Reloads the inventory from the coordinator data root after the spec file changes.
  /// </summary>
  internal void Reload() {
    _inventory = Load();
  }

  public CoordinatorSpecView View => new(
    _inventory.Network,
    _inventory.Server,
    _inventory.Settings,
    _inventory.Agents.AsReadOnly()
  );

  private Inventory Load() {
    return _load();
  }
}
