namespace Drift.Cli.Presentation.Rendering;

internal static class Chars {
  internal const string Warning = "⚠" + EmojiStyle;
  internal const string Globe = "🌐" + EmojiStyle;
  internal const string SatelliteAntenna = "📡" + EmojiStyle;
  internal const string Bulb = "💡" + EmojiStyle;
  internal const string MagnifyingGlass = "🔍" + EmojiStyle;

  // TODO consider these. cross/multiplication doesn't have an emoji version
  internal const string Cross = "✗";
  internal const string Checkmark = "✔";

  /// <summary>
  /// Unicode Variation Selector 16 (VS16): forces emoji-style rendering.
  /// </summary>
  /// <remarks>See https://en.wikipedia.org/wiki/Variation_Selectors_(Unicode_block).</remarks>
  private const string EmojiStyle = "\uFE0F";

  /// <summary>
  /// Unicode Variation Selector 15 (VS15): forces text-style rendering.
  /// </summary>
  /// <remarks>See https://en.wikipedia.org/wiki/Variation_Selectors_(Unicode_block).</remarks>
  private const string TextStyle = "\uFE0E";

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