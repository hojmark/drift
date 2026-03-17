using System.Runtime.InteropServices;

namespace Drift.Cli.Presentation.Rendering;

internal static class Chars {
  internal const string Warning = "⚠" + EmojiStyle;
  internal const string Globe = "🌐" + EmojiStyle;
  internal const string SatelliteAntenna = "📡" + EmojiStyle;
  internal const string Bulb = "💡" + EmojiStyle;
  internal const string MagnifyingGlass = "🔍" + EmojiStyle;

  // TODO consider these. cross/multiplication doesn't have an emoji version
  internal const string Cross = "\u2715";

  // U+2713 (✓) is not in Windows OEM code pages (CP437/CP850), so we use
  // U+221A (√) on Windows, which is available in CP850 (byte 0xFB).
  // On non-Windows platforms (Linux, macOS), U+2713 (✓) is the canonical checkmark.
  internal static readonly string Checkmark =
    RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ? "\u221A" : "\u2713";

  /// <summary>
  /// Unicode Variation Selector 16 (VS16): forces emoji-style rendering.
  /// </summary>
  /// <remarks>See https://en.wikipedia.org/wiki/Variation_Selectors_(Unicode_block).</remarks>
  private const string EmojiStyle = "\uFE0F";

  /// <summary>
  /// Unicode Variation Selector 15 (VS15): forces text-style rendering.
  /// </summary>
  /// <remarks>See https://en.wikipedia.org/wiki/Variation_Selectors_(Unicode_block).</remarks>
#pragma warning disable S1144
  private const string TextStyle = "\uFE0E";
#pragma warning restore S1144

  internal static ICollection<string> All() {
    return new List<string> {
      Warning,
      Globe,
      SatelliteAntenna,
      Bulb,
      MagnifyingGlass,
      Cross,
      Checkmark
    };
  }
}