namespace Drift.Cli.Commands.Scan.Interactive.KeyMaps;

internal class DefaultKeyMap : IKeyMap {
  public UiAction MapKey( ConsoleKey key ) => key switch {
    ConsoleKey.Q => UiAction.Quit,
    ConsoleKey.W => UiAction.ScrollUp,
    ConsoleKey.S => UiAction.ScrollDown,
    ConsoleKey.PageUp => UiAction.ScrollUpPage,
    ConsoleKey.PageDown => UiAction.ScrollDownPage,
    ConsoleKey.UpArrow => UiAction.MoveUp,
    ConsoleKey.DownArrow => UiAction.MoveDown,
    ConsoleKey.Enter or ConsoleKey.Spacebar => UiAction.ToggleSubnet,
    ConsoleKey.R => UiAction.RestartScan,
    ConsoleKey.L => UiAction.ToggleLog,
    _ => UiAction.None
  };
}