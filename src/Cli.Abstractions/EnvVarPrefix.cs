namespace Drift.Cli.Abstractions;

/// <summary>
/// Prefixes for environment variables that override host configuration files. The prefix is stripped and
/// <c>__</c> separates sections, e.g. <c>DRIFT_SERVER_REQUESTLOGGING__ENABLED=false</c> sets <c>requestLogging.enabled</c>.
/// </summary>
public static class EnvVarPrefix {
  public const string Server = "DRIFT_SERVER_";
  public const string Agent = "DRIFT_AGENT_";
}