namespace Drift.Cli.Commands.Global;

internal static class ConsoleExtensions {
  internal static class Text {
    internal static string Bold( string text ) => $"\x1b[1m{text}\x1b[0m";
  }
}