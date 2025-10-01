namespace Drift.Cli.Commands.Scan.Interactive.Input;

internal interface IKeyMap {
  public UiAction Map( ConsoleKey key );
}