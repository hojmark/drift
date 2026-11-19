using Drift.Domain.ExecutionEnvironment;

namespace Drift.Common;

/// <summary>
/// Provides the configured execution environment from the process environment variables.
/// </summary>
public sealed class EnvironmentExecutionEnvironmentProvider : IExecutionEnvironmentProvider {
  /// <inheritdoc />
  public DriftExecutionEnvironment Get() {
    var name = Environment.GetEnvironmentVariable(
      "Drift_ExecutionEnvironment"
    ); // TODO refactor to used shared constant
    return string.IsNullOrWhiteSpace( name ) ||
           !Enum.TryParse( name.Trim(), true, out DriftExecutionEnvironment environment )
      ? DriftExecutionEnvironment.Undefined
      : environment;
  }
}