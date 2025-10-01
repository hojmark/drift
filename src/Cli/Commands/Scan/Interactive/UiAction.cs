namespace Drift.Cli.Commands.Scan.Interactive;

internal enum UiAction {
  None,
  Quit,
  ScrollUp,
  ScrollDown,
  ScrollUpPage,
  ScrollDownPage,
  MoveUp,
  MoveDown,
  ToggleSubnet,
  RestartScan,
  ToggleLog,
  ToggleDebug
}