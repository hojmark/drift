namespace Drift.Cli.Abstractions;

/// <summary>
/// Prefixes for environment variables that override host configuration files.
/// </summary>
public static class EnvVarPrefix {
  public const string Server = "DRIFT_SERVER_";
  public const string Agent = "DRIFT_AGENT_";
}