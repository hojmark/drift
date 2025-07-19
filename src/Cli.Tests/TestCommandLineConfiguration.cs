using System.CommandLine;

namespace Drift.Cli.Tests;

internal static class TestCommandLineConfiguration {
  internal static CommandLineConfiguration Create() {
    var rootCommand = RootCommandFactory.Create( toConsole: false );

    return new CommandLineConfiguration( rootCommand ) { Output = new StringWriter(), Error = new StringWriter() };
  }
}