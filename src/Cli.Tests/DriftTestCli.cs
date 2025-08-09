using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Cli.Tests;

internal static class DriftTestCli {
  internal static async Task<(int ExitCode, TextWriter Output, TextWriter Error )> InvokeFromTestAsync(
    string args,
    Action<IServiceCollection>? configureServices = null,
    RootCommandFactory.CommandRegistration[]? customCommands = null
  ) {
    var output = new StringWriter();
    var error = new StringWriter();

    var configureCommandLineConfig = ( CommandLineConfiguration config ) => {
      config.Output = output;
      config.Error = error;
    };

    return (
      await DriftCli.InvokeAsync(
        CommandLineParser.SplitCommandLine( args ).ToArray(),
        false,
        true,
        configureServices,
        customCommands,
        configureCommandLineConfig
      ),
      output,
      error
    );
  }
}