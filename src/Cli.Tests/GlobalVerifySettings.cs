using System.Runtime.CompilerServices;

namespace Drift.Cli.Tests;

// Normalize platform-specific characters before snapshot comparison.
// Chars.Checkmark returns "√" (U+221A) on Windows (CP850-compatible) and "✓" (U+2713) on non-Windows platforms.
// The verified snapshots use the non-Windows/canonical form (✓), so we scrub the Windows variant here.
internal static class GlobalVerifySettings {
  [ModuleInitializer]
  internal static void Initialize() {
    VerifierSettings.AddScrubber( sb => sb.Replace( "\u221A", "\u2713" ) );
  }
}
