namespace Drift.Cli.Tests.Utils;

internal sealed class CliCommandResult {
  internal required int ExitCode {
    get;
    init;
  }

  internal required TextWriter Output {
    get;
    init;
  }

  internal required TextWriter Error {
    get;
    init;
  }

  public void Deconstruct( out int exitCode, out TextWriter output, out TextWriter error ) {
    exitCode = ExitCode;
    output = Output;
    error = Error;
  }
}