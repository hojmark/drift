using Drift.Coordinator.Services.State;
using Drift.Spec.Schema;
using Drift.Spec.Validation;

namespace Drift.Coordinator.Services.Spec;

/// <summary>
/// Validates and persists the coordinator's declarative specification.
/// </summary>
public sealed class SpecService(
  ICoordinatorSpecStore specStore,
  CoordinatorSpec coordinatorSpec
) {
  public string GetYaml() => specStore.LoadYaml();

  public async Task<ValidationResult> ReplaceAsync( string yaml, CancellationToken cancellationToken ) {
    var validation = SpecValidator.Validate( yaml, SpecVersion.V1_preview );

    if ( !validation.IsValid ) {
      return validation;
    }

    await specStore.ReplaceAsync( yaml, cancellationToken );

    coordinatorSpec.Reload();

    return validation;
  }
}