using System.Diagnostics;

namespace Drift.Build.Utilities;

public static class CommandRunner {
  public static async Task<string> RunAsync( string command, string arguments ) {
    using var process = Process.Start( new ProcessStartInfo {
      FileName = command,
      Arguments = arguments,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    } );

    if ( process == null ) {
      throw new Exception( $"Could not start process '{command} {arguments}'" );
    }

    await process.WaitForExitAsync();

    string output = await process.StandardOutput.ReadToEndAsync();
    string error = await process.StandardError.ReadToEndAsync();

    if ( process.ExitCode != 0 ) {
      throw new Exception( $"Process '{command} {arguments}' failed:\n{error}" );
    }

    return output;
  }
}