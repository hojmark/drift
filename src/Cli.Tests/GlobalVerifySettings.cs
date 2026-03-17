using System.Runtime.CompilerServices;
using Drift.TestUtilities;

namespace Drift.Cli.Tests;

internal static class GlobalVerifySettings {
  [ModuleInitializer]
  internal static void Initialize() {
    VerifySettingsDefaults.AddCheckmarkScrubber();
  }
}
