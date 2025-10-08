using System.Diagnostics.CodeAnalysis;

namespace Drift.Cli.Abstractions;

[SuppressMessage( "ReSharper", "InconsistentNaming", Justification = "Environment variable name" )]
public enum EnvVar {
  DRIFT_ENVIRONMENT
}

public enum Environment {
  Undefined = 0,
  Container = 1,
  Other = 2
}

public static class CurrentEnvironment {
  public static Environment Get() {
    var envVar = System.Environment.GetEnvironmentVariable( nameof(EnvVar.DRIFT_ENVIRONMENT) );

    if ( string.IsNullOrWhiteSpace( envVar ) ) {
      return Environment.Undefined;
    }

    return Enum.TryParse<Environment>( envVar.Trim(), true, out var env )
      ? env
      : Environment.Undefined;
  }
}