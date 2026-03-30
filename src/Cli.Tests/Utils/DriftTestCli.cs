using System.CommandLine;
using System.CommandLine.Parsing;
using Drift.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Cli.Tests.Utils;

internal static class DriftTestCli {
  private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds( 7 );

  // Console.SetOut/SetError mutates global state, so only one invocation may redirect it at a time.
  private static readonly SemaphoreSlim ConsoleRedirectLock = new(1, 1);

  internal static async Task<(int ExitCode, TextWriter Output, TextWriter Error )> InvokeFromTestAsync(
    string args,
    Action<IServiceCollection>? configureServices = null,
    RootCommandFactory.CommandRegistration[]? customCommands = null,
    CancellationToken cancellationToken = default
  ) {
    var token = cancellationToken;
    CancellationTokenSource? cancellationTokenSource = null;

    if ( cancellationToken == default ) {
      cancellationTokenSource = new CancellationTokenSource( DefaultCommandTimeout );
      token = cancellationTokenSource.Token;
    }

    var output = new StringWriter();
    var error = new StringWriter();

    void ConfigureInvocation( InvocationConfiguration config ) {
      config.Output = output;
      config.Error = error;
    }

    /*
     * Most output is written to the InvocationConfiguration's TextWriters, but a few errors may be written to
     * Console.Out/Error when DI is not yet available.
     */
    await ConsoleRedirectLock.WaitAsync( token );

    var previousOut = Console.Out;
    var previousErr = Console.Error;
    Console.SetOut( output );
    Console.SetError( error );

    try {
      return (
        await DriftCli.InvokeAsync(
          CommandLineParser.SplitCommandLine( args ).ToArray(),
          false,
          true,
          configureServices,
          customCommands,
          ConfigureInvocation,
          token
        ),
        output,
        error
      );
    }
    finally {
      Console.SetOut( previousOut );
      Console.SetError( previousErr );
      ConsoleRedirectLock.Release();
      cancellationTokenSource?.Dispose();
    }
  }
}