namespace Drift.Cli.Commands.Common;

// TODO consider grep?
/// <summary>
/// Formats for console output.
/// </summary>
internal enum OutputFormat {
  /// <summary>
  /// Standard console output (default).
  /// </summary>
  Normal = 1,

  /// <summary>
  /// Log-style console output.
  /// </summary>
  Log = 2

  //TODO support
  /// <summary>
  /// JSON format console output.
  /// </summary>
  //Json = 3,
}