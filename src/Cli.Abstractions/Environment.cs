namespace Drift.Cli.Abstractions;

public enum DriftEnv {
  Undefined = 0,
  Container = 1
}

public static class Environment {
  public static DriftEnv GetCurrent() {
    var envVar = System.Environment.GetEnvironmentVariable( nameof(EnvVar.DRIFT_ENVIRONMENT) );

    return Get( envVar );
  }

  internal static DriftEnv Get( string? name ) {
    if ( string.IsNullOrWhiteSpace( name ) ) {
      return DriftEnv.Undefined;
    }

    return Enum.TryParse<DriftEnv>( name.Trim(), true, out var env )
      ? env
      : DriftEnv.Undefined;
  }
}