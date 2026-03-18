namespace Drift.Cli.Infrastructure;

internal static class Bootstrapper {
  internal static Task BootstrapAsync() {
    // Ensure the process writes UTF-8 bytes to stdout/stderr on all platforms.
    // On Windows the default is the OEM code page (CP437/CP850), which cannot represent
    // emoji or many Unicode characters — they would be silently replaced with '?'.
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    Console.InputEncoding = System.Text.Encoding.UTF8;

    return Task.CompletedTask;
  }
}