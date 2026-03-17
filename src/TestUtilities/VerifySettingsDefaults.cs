namespace Drift.TestUtilities;

public static class VerifySettingsDefaults {
  /// <summary>
  /// Scrubs the Windows checkmark variant so snapshots match across platforms.
  /// </summary>
  /// <remarks>
  /// Windows OEM code pages (CP437/CP850) lack U+2713 (✓ CHECK MARK), so <c>Chars.Checkmark</c>
  /// emits U+221A (√ SQUARE ROOT, byte 0xFB in CP850) instead. Verified snapshots are stored in
  /// the canonical ✓ form, so this scrubber normalises the Windows variant before comparison.
  /// </remarks>
  public static void AddCheckmarkScrubber() {
    VerifierSettings.AddScrubber( sb => sb.Replace( "\u221A", "\u2713" ) );
  }
}
