using Drift.Cli.Abstractions;
using Drift.Domain.ExecutionEnvironment;

namespace Drift.Cli;

internal static class ExecutionEnvironment {
  internal static DriftExecutionEnvironment GetCurrent() {
    var envVar = Environment.GetEnvironmentVariable( nameof(EnvVar.DRIFT_EXECUTION__ENVIRONMENT) );

    return Get( envVar );
  }

  internal static DriftExecutionEnvironment Get( string? name ) {
    if ( string.IsNullOrWhiteSpace( name ) ) {
      return DriftExecutionEnvironment.Undefined;
    }

    return Enum.TryParse<DriftExecutionEnvironment>( name.Trim(), true, out var env )
      ? env
      : DriftExecutionEnvironment.Undefined;
  }
}