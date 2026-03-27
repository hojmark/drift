using System.Diagnostics.CodeAnalysis;

namespace Drift.Cli.Abstractions;

[SuppressMessage( "ReSharper", "InconsistentNaming", Justification = "Environment variable name" )]
public enum EnvVar {
  DRIFT_EXECUTION__ENVIRONMENT,
  DRIFT_CONFIG_DIR,
  NO_COLOR // TODO implement
}