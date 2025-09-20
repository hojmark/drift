namespace Drift.Cli.Commands.Scan.Interactive;

internal enum InputAction {
  None,
  Quit,
  ScrollUp,
  ScrollDown,
  MoveUp,
  MoveDown,
  Expand,
  Collapse,
  ToggleSelected,
  ToggleByIndex,
  RestartScan,
  ToggleLog
}