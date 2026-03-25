using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Drift.Common;

[SuppressMessage( "Major Code Smell", "S3264:Events should be invoked", Justification = "False positive" )]
public class ToolWrapper( string toolPath, Dictionary<string, string?>? environment = null ) {
  private readonly string _toolPath = toolPath ?? throw new ArgumentNullException( nameof(toolPath) );

  public event Action<string?>? OutputDataReceived;

  public event Action<string?>? ErrorDataReceived;

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
      StandardOutputEncoding = new UTF8Encoding( false ),
      StandardErrorEncoding = new UTF8Encoding( false ),
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

    logger?.LogTrace( "Executing: {Tool} {Arguments}", _toolPath, arguments );

    process.Start();

    // Read stdout and stderr concurrently using StreamReader directly, which respects
    // StandardOutputEncoding / StandardErrorEncoding. Using BeginOutputReadLine() is
    // avoided because it can ignore StandardOutputEncoding on some Windows configurations
    // and decode bytes using the system default code page instead.
    var outputTask = ReadStreamAsync( process.StandardOutput, OutputDataReceived );
    var errorTask = ReadStreamAsync( process.StandardError, ErrorDataReceived );

    int exitCode = int.MinValue;
    var cancelled = false;

    try {
      await process.WaitForExitAsync( cancellationToken );
      exitCode = process.ExitCode;
    }
    catch ( OperationCanceledException ) {
      cancelled = true;
      try {
        process.Kill( entireProcessTree: true );
      }
      catch {
        // Process may have already exited
      }
    }

    var stdOut = await outputTask;
    var errOut = await errorTask;

    return ( stdOut, errOut, exitCode, cancelled );
  }

  private static async Task<string> ReadStreamAsync(
    StreamReader reader,
    Action<string?>? lineHandler
  ) {
    var sb = new StringBuilder();
    string? line;

    while ( ( line = await reader.ReadLineAsync() ) != null ) {
      sb.AppendLine( line );
      lineHandler?.Invoke( line );
    }

    return sb.ToString();
  }
}
