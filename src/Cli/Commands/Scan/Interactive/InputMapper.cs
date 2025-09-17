namespace Drift.Cli.Commands.Scan.Interactive;

public static class InputMapper {
  public static InputAction MapKey( ConsoleKey key ) => key switch {
    ConsoleKey.Q => InputAction.Quit,
    ConsoleKey.W => InputAction.ScrollUp,
    ConsoleKey.S => InputAction.ScrollDown,
    ConsoleKey.UpArrow => InputAction.MoveUp,
    ConsoleKey.DownArrow => InputAction.MoveDown,
    //ConsoleKey.LeftArrow => InputAction.Collapse,
    //ConsoleKey.RightArrow => InputAction.Expand,
    ConsoleKey.Enter or ConsoleKey.Spacebar => InputAction.ToggleSelected,
    >= ConsoleKey.D1 and <= ConsoleKey.D9 => InputAction.ToggleByIndex,
    ConsoleKey.R => InputAction.RestartScan,
    ConsoleKey.L => InputAction.ToggleLog,
    _ => InputAction.None
  };

  public static int GetNumericIndex( ConsoleKey key ) => (int) key - (int) ConsoleKey.D1;
}