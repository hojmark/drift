namespace Drift.Cli.Tests;

internal class TestConsole {
  internal TextWriter Out {
    get;
  } = new StringWriter();

  internal TextWriter Error {
    get;
  } = new StringWriter();
}