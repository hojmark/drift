using Drift.Cli.Output.Abstractions;

namespace Drift.Cli.Commands.Scan.Interactive.Input;

internal interface ILogReader {
  event EventHandler<string>? LogUpdated;
  Task StartAsync( CancellationToken cancellationToken );
}

internal class LogWatcher( IOutputManager outputManager ) : ILogReader {
  private Task? _readTask;
  public event EventHandler<string>? LogUpdated;

  public Task StartAsync( CancellationToken cancellationToken ) {
    _readTask = Task.Run( async () => {
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

  protected virtual void OnLogUpdated( string line ) {
    LogUpdated?.Invoke( this, line );
  }
}