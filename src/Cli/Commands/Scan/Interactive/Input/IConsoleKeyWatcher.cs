namespace Drift.Cli.Commands.Scan.Interactive.Input;

/// <summary>
/// Receives keyboard input for an interactive console session.
/// </summary>
internal interface IConsoleKeyWatcher : IAsyncDisposable {
  /// <summary>
  /// Asynchronously waits until at least one key is available.
  /// </summary>
  /// <returns>A task that completes when a key is available.</returns>
  Task WaitForKeyAsync();

  /// <summary>
  /// Removes and returns the next buffered key if one is available.
  /// </summary>
  /// <returns>The next key, or <see langword="null"/> when the buffer is empty.</returns>
  ConsoleKey? Consume();
}