namespace Drift.Cli.SpecFile;

/// <summary>
/// Thrown when a spec file exists but cannot be loaded (e.g. it is not valid YAML or does not match the spec
/// structure). The cause has already been reported to the user when this is thrown.
/// </summary>
// Justified: no consumers as this is an executable assembly
#pragma warning disable S3871
internal sealed class InvalidSpecException( string message, Exception innerException )
#pragma warning restore S3871
  : Exception( message, innerException );