namespace Drift.Cli.Output.Normal;

internal static class TextHelper {
  internal static string Bold( string text ) => $"\x1b[1m{text}\x1b[0m";
}