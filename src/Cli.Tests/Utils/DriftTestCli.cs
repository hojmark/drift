using System.CommandLine;
using System.CommandLine.Parsing;
using Drift.Cli.Commands.Agent.Subcommands;
using Drift.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Cli.Tests.Utils;

internal static class DriftTestCli {
  private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds( 7 );

  internal static async Task<CliCommandResult> InvokeAsync(
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
      var exitCode = await DriftCli.InvokeAsync(
        CommandLineParser.SplitCommandLine( args ).ToArray(),
        false,
        true,
        configureServices,
        customCommands,
        ConfigureInvocation,
        token
      );

      return new CliCommandResult { ExitCode = exitCode, Output = output, Error = error };
    }
    finally {
      cancellationTokenSource?.Dispose();
    }
  }

  internal static RunningCliCommand StartAsync(
    string args,
    Action<IServiceCollection> configureServices,
    CancellationToken cancellationToken
  ) {
    var cts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );

    var task = InvokeAsync(
      args,
      configureServices,
      cancellationToken: cts.Token
    );

    return new RunningCliCommand( task, cts );
  }

  /// <summary>
  /// Starts a new agent asynchronously and returns tasks that complete when it has started.
  /// </summary>
  internal static async Task<RunningCliCommand> StartAgentAsync(
    string args,
    CancellationToken cancellationToken,
    Action<IServiceCollection>? configureServices = null
  ) {
    var readyTcs = new AgentLifetime();

    var command = StartAsync(
      "agent start " + args,
      services => {
        services.AddSingleton( readyTcs );
        configureServices?.Invoke( services );
      },
      cancellationToken
    );

    // Wait for either readiness or command exit
    var completed = await Task.WhenAny( readyTcs.Ready.Task, command.Completion );

    if ( completed == command.Completion ) {
      var com = await command.Completion;
      throw new InvalidOperationException( "Command exited before agent was started. Details:\n" + com.Error );
    }

    return command;
  }
}