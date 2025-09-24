namespace Drift.Cli.Commands.Scan.Interactive.KeyMaps;

internal interface IKeyMap {
  public UiAction Map( ConsoleKey key );
}