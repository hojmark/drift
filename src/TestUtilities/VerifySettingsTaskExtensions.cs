using System.Globalization;
using System.Text.RegularExpressions;

namespace Drift.TestUtilities;

public static class VerifySettingsTaskExtensions {
  public static SettingsTask ScrubLogOutputTime( this SettingsTask settings ) {
    return settings
      .ScrubInlineDateTimes( "HH:mm:ss", CultureInfo.InvariantCulture )
      .ScrubLinesWithReplace( line => Regex.Replace( line, @"DateTime_\d+", "<time>" ), ScrubberLocation.Last );
  }
}