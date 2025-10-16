using System.CommandLine;
using System.CommandLine.Parsing;
using Drift.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Cli.Tests.Utils;

internal static class DriftTestCli {
  private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds( 7 );

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
      cancellationTokenSource?.Dispose();
    }
  }
}