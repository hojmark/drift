namespace Drift.Cli.E2ETests.Utils;

internal enum EnvVar {
  // ReSharper disable once InconsistentNaming
  DRIFT_BINARY_PATH
}

internal static class EnvironmentVariable {
  internal static string GetOrThrow( EnvVar variable ) {
    return Environment.GetEnvironmentVariable( variable.ToString().ToUpperInvariant() ) ??
           throw new Exception( $"Environment variable not set: {variable}" );
  }

  internal static bool IsCi() {
    return Environment.GetEnvironmentVariable( "CI" ) == "true";
  }
}