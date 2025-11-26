using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Rendering;

namespace Drift.Cli;

internal static class DriftCli {
  internal static async Task<int> InvokeAsync(
    string[] args,
    bool toConsole = true,
    bool plainConsole = false,
    Action<IServiceCollection>? configureServices = null,
    RootCommandFactory.CommandRegistration[]? customCommands = null,
    Action<InvocationConfiguration>? configureInvocation = null,
    CancellationToken cancellationToken = default
  ) {
    // Justification: intentionally using the most basic output form to make sure the error is surfaced, no matter what code fails
#pragma warning disable RS0030
    var error = Console.Error;

    try {
      await Bootstrapper.BootstrapAsync();

      var rootCommand = RootCommandFactory.Create(
        toConsole: toConsole,
        plainConsole: plainConsole,
        configureServices,
        customCommands
      );

      var config = new InvocationConfiguration { EnableDefaultExceptionHandler = false };

      configureInvocation?.Invoke( config );

      error = config.Error;

      var parseResult = rootCommand.Parse( args );

      return await parseResult.InvokeAsync( config, cancellationToken );
    }
    catch ( Exception e ) {
      Console.ForegroundColor = ConsoleColor.DarkRed;
      await error.WriteAsync( $"{Chars.Cross} " );
      Console.ForegroundColor = ConsoleColor.Red;
      await error.WriteLineAsync( e.Message );
      Console.ForegroundColor = ConsoleColor.Gray;
      await error.WriteLineAsync( e.StackTrace );
      Console.ResetColor();

      return ExitCodes.UnknownError;
    }
#pragma warning restore RS0030
  }
}