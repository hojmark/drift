using Drift.Cli.Presentation.Rendering;

namespace Drift.Cli.Commands.Scan.Interactive.Ui;

internal static class DisplayValueExtensions {
  internal static string PadRight( this DisplayValue displayValue, int totalWidth ) {
    var padLength = totalWidth - displayValue.WithoutMarkup.Length;

    return padLength > 0
      ? displayValue.Value + new string( ' ', padLength )
      : displayValue.Value;
  }
}