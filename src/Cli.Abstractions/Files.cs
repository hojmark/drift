namespace Drift.Cli.Abstractions;

public static class Files {
  /// <summary>
  /// Configuration file for the Drift CLI, located in the config directory.
  /// </summary>
  /// <seealso cref="ServerConfigFileName"/>
  /// <seealso cref="AgentConfigFileName"/>
  public const string SettingsFileName = "settings.json";

  /// <summary>
  /// Configuration file for <c>drift server start</c>, located in the config directory.
  /// </summary>
  public const string ServerConfigFileName = "server.json";

  /// <summary>
  /// Configuration file for <c>drift agent start</c>, located in the config directory.
  /// </summary>
  public const string AgentConfigFileName = "agent.json";
}