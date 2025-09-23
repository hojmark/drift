using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Drift.Utils;

public class ToolWrapper( string toolPath, Dictionary<string, string?>? environment = null ) {
  private readonly string _toolPath = toolPath ?? throw new ArgumentNullException( nameof(toolPath) );

  public async Task<(string StdOut, string ErrOut, int ExitCode, bool Cancelled)> ExecuteAsync(
    string arguments,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  ) {
    var startInfo = new ProcessStartInfo {
      FileName = _toolPath,
      Arguments = arguments,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false, // Do not use the OS shell
      CreateNoWindow = true
    };

    if ( environment != null ) {
      foreach ( var (key, value) in environment ) {
        startInfo.Environment[key] = value;
      }
    }

    using var process = new Process();
    process.StartInfo = startInfo;

    var output = new StringBuilder();
    var error = new StringBuilder();

    process.OutputDataReceived += ( sender, args ) => {
      if ( args.Data != null ) {
        output.AppendLine( args.Data );
      }
    };
    process.ErrorDataReceived += ( sender, args ) => {
      if ( args.Data != null ) {
        error.AppendLine( args.Data );
      }
    };

    logger?.LogDebug( "Executing: {Tool} {Arguments}", _toolPath, arguments );

    process.Start();

    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    int exitCode = int.MinValue;
    var cancelled = false;

    try {
      await process.WaitForExitAsync( cancellationToken );
      exitCode = process.ExitCode;
    }
    catch ( OperationCanceledException ) {
      cancelled = true;
    }

    process.CancelOutputRead();
    process.CancelErrorRead();

    return ( output.ToString(), error.ToString(), exitCode, cancelled );
  }
}