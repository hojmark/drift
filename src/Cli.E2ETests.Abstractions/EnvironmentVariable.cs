using System.Diagnostics.CodeAnalysis;

namespace Drift.Cli.E2ETests.Abstractions;

[SuppressMessage( "ReSharper", "InconsistentNaming", Justification = "Environment variable name" )]
public enum EnvVar {
  DRIFT_BINARY_PATH,
  //TODO USE THIS
  DRIFT_CONTAINER_IMAGE_TAG
}

public static class EnvironmentVariable {
  public static string GetOrThrow( EnvVar variable ) {
    return Environment.GetEnvironmentVariable( variable.ToString().ToUpperInvariant() ) ??
           throw new Exception( $"Environment variable not set: {variable}" );
  }
}