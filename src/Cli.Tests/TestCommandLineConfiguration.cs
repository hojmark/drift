using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Cli.Tests;

internal static class TestCommandLineConfiguration {
  internal static CommandLineConfiguration Create( Action<IServiceCollection>? configureServices = null ) {
    var rootCommand = RootCommandFactory.Create( toConsole: false, configureServices );

    return new CommandLineConfiguration( rootCommand ) { Output = new StringWriter(), Error = new StringWriter() };
  }
}