using Drift.Cli.Settings.V1_preview.Appearance;

namespace Drift.Cli.Presentation.Console;

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

  // TODO support
  /*
  /// <summary>
  /// JSON format console output.
  /// </summary>
  // Json = 3,
  */
}

internal static class OutputFormatSettingExtensions {
  internal static OutputFormat ToOutputFormat( this OutputFormatSetting setting ) {
    return setting switch {
      OutputFormatSetting.Default or OutputFormatSetting.Normal => OutputFormat.Normal,
      OutputFormatSetting.Log => OutputFormat.Log,
      _ => throw new ArgumentOutOfRangeException( nameof(setting), setting, null )
    };
  }
}