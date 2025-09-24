using Drift.Cli.Presentation.Console.Managers.Abstractions;

namespace Drift.Cli.Commands.Scan.Interactive.Input;

internal sealed class LogWatcher( IOutputManager outputManager ) : IDisposable {
  private Task? _readTask;

  public event EventHandler<string>? LogUpdated;

  public Task StartAsync( CancellationToken cancellationToken ) {
    _readTask = Task.Run(
      async () => {
        var reader = outputManager.GetReader();

        while ( !cancellationToken.IsCancellationRequested ) {
          var line = await reader.ReadLineAsync( cancellationToken );

          if ( !string.IsNullOrEmpty( line ) ) {
            OnLogUpdated( line );
          }
          else {
            try {
              await Task.Delay( 50, cancellationToken );
            }
            catch ( TaskCanceledException ) {
              // Swallow cancellation
              break;
            }
          }
        }
      }, cancellationToken );

    return Task.CompletedTask;
  }

  public void Dispose() {
    _readTask?.Dispose();
  }

  private void OnLogUpdated( string line ) {
    LogUpdated?.Invoke( this, line );
  }
}