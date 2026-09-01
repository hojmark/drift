namespace Drift.Cli.Commands.Scan.Interactive.Input;

/// <summary>
/// Waits for changes to the interactive console dimensions.
/// </summary>
internal interface IConsoleResizeWatcher : IDisposable {
  /// <summary>
  /// Asynchronously waits until the console dimensions change.
  /// </summary>
  /// <returns>A task that completes when a resize is detected.</returns>
  Task WaitForResizeAsync();
}