namespace Drift.Cli.Commands.Scan.Interactive.KeyMaps;

internal interface IKeyMap {
  public UiAction MapKey( ConsoleKey key );
}