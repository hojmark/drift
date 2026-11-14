namespace Drift.Cli.SpecFile;

/// <summary>
/// Thrown when a spec file exists but cannot be loaded (e.g. it is not valid YAML or does not match the spec
/// structure). The cause has already been reported to the user when this is thrown.
/// </summary>
internal sealed class InvalidSpecException( string message, Exception innerException )
  : Exception( message, innerException );