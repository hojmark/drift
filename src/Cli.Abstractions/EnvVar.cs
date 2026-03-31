using System.Diagnostics.CodeAnalysis;

namespace Drift.Cli.Abstractions;

[SuppressMessage( "ReSharper", "InconsistentNaming", Justification = "Environment variable name" )]
public enum EnvVar {
  Drift_ExecutionEnvironment,
  Drift_ConfigDir,
  NO_COLOR // TODO implement
}