using System.CommandLine;
using System.CommandLine.Parsing;
using Drift.Cli.Commands;
using Drift.Cli.Infrastructure;
using Drift.Cli.Settings.Tests;
using Drift.Common.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Cli.Tests.Utils;

internal static class DriftTestCli {
  private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds( 7 );

  // Console.SetOut/SetError mutates global state, so only one invocation may redirect it at a time.
  private static readonly SemaphoreSlim ConsoleRedirectLock = new(1, 1);

  internal static async Task<CliCommandResult> InvokeAsync(
    string args,
    Action<IServiceCollection>? configureCliServices = null,
    Action<IServiceCollection>? configureAgentHostServices = null,
    Action<IServiceCollection>? configureCoordinatorHostServices = null,
    RootCommandFactory.CommandRegistration[]? customCommands = null,
    bool redirectConsole = true,
    IDriftSettingsLocation? settingsLocation = null,
    CancellationToken cancellationToken = default
  ) {
    settingsLocation ??= new TemporarySettingsLocation();
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

    // Register test settings
    Action<IServiceCollection> wrappedConfigure = services => {
      services.AddSingleton( settingsLocation );
      configureCliServices?.Invoke( services );
    };

    /*
     * Most output is written to the InvocationConfiguration's TextWriters, but a few errors may be written to
     * Console.Out/Error when DI is not yet available.
     *
     * Long-running background processes (e.g. agents) must NOT hold ConsoleRedirectLock for their
     * entire lifetime — doing so would prevent any other InvokeAsync call from acquiring the lock
     * (deadlock when running multiple agents or a scan alongside agents). Pass redirectConsole: false
     * to skip the lock and Console.SetOut/SetError for those cases.
     */
    if ( redirectConsole ) {
      await ConsoleRedirectLock.WaitAsync( token );

      var previousOut = Console.Out;
      var previousErr = Console.Error;
      Console.SetOut( output );
      Console.SetError( error );

      try {
        var exitCode = await DriftCli.InvokeAsync(
          CommandLineParser.SplitCommandLine( args ).ToArray(),
          toConsole: false,
          plainConsole: true,
          configureCliServices: wrappedConfigure,
          configureAgentHostServices: configureAgentHostServices,
          configureCoordinatorHostServices: configureCoordinatorHostServices,
          customCommands: customCommands,
          configureInvocation: ConfigureInvocation,
          cancellationToken: token
        );

        return new CliCommandResult { ExitCode = exitCode, Output = output, Error = error };
      }
      finally {
        Console.SetOut( previousOut );
        Console.SetError( previousErr );
        ConsoleRedirectLock.Release();
        cancellationTokenSource?.Dispose();
      }
    }

    try {
      var exitCode = await DriftCli.InvokeAsync(
        CommandLineParser.SplitCommandLine( args ).ToArray(),
        toConsole: false,
        plainConsole: true,
        configureCliServices: wrappedConfigure,
        configureAgentHostServices: configureAgentHostServices,
        configureCoordinatorHostServices: configureCoordinatorHostServices,
        customCommands: customCommands,
        configureInvocation: ConfigureInvocation,
        cancellationToken: token
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
    CancellationToken cancellationToken,
    Action<IServiceCollection>? configureAgentHostServices = null,
    Action<IServiceCollection>? configureCoordinatorHostServices = null
  ) {
    var cts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );

    var task = InvokeAsync(
      args,
      configureCliServices: configureServices,
      configureAgentHostServices: configureAgentHostServices,
      configureCoordinatorHostServices: configureCoordinatorHostServices,
      redirectConsole: false,
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
    Action<IServiceCollection>? configure = null
  ) {
    var lifetime = new NestedHostLifetime();

    var command = StartAsync(
      "agent start " + args,
      services => {
        services.AddSingleton( lifetime );
      },
      cancellationToken,
      configureAgentHostServices: configure
    );

    // Wait for either readiness or command exit
    var completed = await Task.WhenAny( lifetime.Ready.Task, command.Completion );

    if ( completed == command.Completion ) {
      var result = await command.Completion;
      throw new InvalidOperationException( "Command exited before agent was started. Details:\n" + result.Error );
    }

    return command;
  }

  /// <summary>
  /// Starts a new server asynchronously and returns tasks that complete when it has started.
  /// </summary>
  internal static async Task<RunningCliCommand> StartServerAsync(
    string args,
    CancellationToken cancellationToken,
    Action<IServiceCollection>? configure = null
  ) {
    var lifetime = new NestedHostLifetime();

    var command = StartAsync(
      "server start " + args,
      services => {
        services.AddSingleton( lifetime );
      },
      cancellationToken,
      configureCoordinatorHostServices: configure
    );

    // Wait for either readiness or command exit
    var completed = await Task.WhenAny( lifetime.Ready.Task, command.Completion );

    if ( completed == command.Completion ) {
      var result = await command.Completion;
      throw new InvalidOperationException( "Command exited before server was started. Details:\n" + result.Error );
    }

    return command;
  }
}