using System.Globalization;
using System.Text.RegularExpressions;

namespace Drift.TestUtilities;

public static partial class VerifySettingsTaskExtensions {
  extension( SettingsTask settings ) {
    public SettingsTask ScrubLogOutputTime() {
      return settings
        .ScrubInlineDateTimes( "HH:mm:ss", CultureInfo.InvariantCulture )
        .ScrubLinesWithReplace( line => Regex.Replace( line, @"DateTime_\d+", "<time>" ), ScrubberLocation.Last );
    }

    public SettingsTask ScrubGuid() {
      return settings
        .ScrubLinesWithReplace( line => GuidRegex().Replace( line, "<guid>" ) );
    }

    public SettingsTask ScrubDataDirectory() {
      return settings
        .ScrubLinesWithReplace( line =>
          Regex.Replace( line, "Data directory: .+", "Data directory: <data-directory>" )
        );
    }

    public SettingsTask ScrubPort() {
      return settings
        .ScrubLinesWithReplace( line =>
          Regex.Replace( line, "Port: .+", "Port: <port>" )
        );
    }

    public SettingsTask ScrubVersion() {
      return settings
        .ScrubLinesWithReplace( line =>
          Regex.Replace( line, "Version: .+", "Version: <version>" )
        );
    }
  }

  [GeneratedRegex( "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}" )]
  private static partial Regex GuidRegex();
}