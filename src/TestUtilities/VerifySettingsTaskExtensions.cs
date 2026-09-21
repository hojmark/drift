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
  }

  [GeneratedRegex( "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}" )]
  private static partial Regex GuidRegex();
}