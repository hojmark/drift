using System.Runtime.CompilerServices;
using Drift.TestUtilities;

namespace Drift.Cli.E2ETests.Container;

internal static class GlobalVerifySettings {
  [ModuleInitializer]
  internal static void Initialize() {
    VerifySettingsDefaults.AddCheckmarkScrubber();
  }
}
