using System.Net;
using System.Net.Sockets;
using Drift.Agent.Host;
using Drift.Cli.Abstractions;
using Drift.Cli.Settings.Serialization;
using Drift.Cli.Settings.Tests;
using Drift.Cli.Settings.V1_preview;
using Drift.Cli.Settings.V1_preview.Environments;
using Drift.Cli.Tests.Utils;
using Drift.Cli.Tests.Utils.Coordinator;
using Drift.Coordinator.Host;
using Drift.Coordinator.Services.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Cli.Tests.Commands;

internal sealed class StatusCommandTests {
  private ISettingsLocation SettingsLocation {
    get;
  } = new TemporarySettingsLocation();

  [TearDown]
  public void TearDown() {
    var settingsDirectory = SettingsLocation.Directory;
    if ( Directory.Exists( settingsDirectory ) ) {
      Directory.Delete( settingsDirectory, true );
    }
  }

  [Test]
  public async Task Status_NoEnvironments_ShowsLocalStatusAndAddresses() {
    var (exitCode, output, error) = await DriftTestCli.InvokeAsync(
      "status",
      settingsLocation: SettingsLocation
    );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( error.ToString(), Is.Empty );
      Assert.That( output.ToString(), Does.Contain( "Data directory:" ) );
      Assert.That( output.ToString(), Does.Contain( "Config directory:" ) );
      Assert.That( output.ToString(), Does.Contain( "Local addresses:" ) );
      Assert.That( output.ToString(), Does.Contain( "local @ local" ) );
      Assert.That( output.ToString(), Does.Contain( "Status: Local scanning" ) );
    }
  }

  [Test]
  public async Task Status_UnreachableEnvironment_ShowsEnvironmentErrorAndKeepsSuccessExitCode() {
    var settings = new CliSettings {
      ActiveEnvironment = "site-a", Environments = [new EnvironmentSetting( "site-a", "http://127.0.0.1:1" )]
    };
    settings.Write( NullLogger.Instance, SettingsLocation );

    var (exitCode, output, error) = await DriftTestCli.InvokeAsync(
      "status",
      settingsLocation: SettingsLocation
    );
    var rendered = output.ToString();

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( error.ToString(), Is.Empty );
      Assert.That( rendered, Does.Contain( "site-a @ http://127.0.0.1:1" ) );
      Assert.That( rendered, Does.Contain( "Status: Unavailable" ) );
      Assert.That( rendered, Does.Contain( "Connection refused (127.0.0.1:1)" ) );
    }
  }

  [Test]
  public async Task Status_MultipleEnvironments_ShowsOnlyTheActiveEnvironment() {
    var settings = new CliSettings {
      ActiveEnvironment = "site-b",
      Environments = [
        new EnvironmentSetting( "site-a", "http://127.0.0.1:1" ),
        new EnvironmentSetting( "site-b", "http://127.0.0.1:1" )
      ]
    };
    settings.Write( NullLogger.Instance, SettingsLocation );

    var (exitCode, output, error) = await DriftTestCli.InvokeAsync(
      "status",
      settingsLocation: SettingsLocation
    );
    var rendered = output.ToString();

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( error.ToString(), Is.Empty );
      Assert.That( rendered, Does.Contain( "site-b @ http://127.0.0.1:1" ) );
      Assert.That( rendered, Does.Not.Contain( "site-a @" ) );
      Assert.That( rendered, Does.Not.Contain( "* local @ local" ) );
    }
  }

  [Test]
  public async Task Status_UnresponsiveEnvironment_ShowsTimeoutAndKeepsSuccessExitCode() {
    using var listener = new TcpListener( IPAddress.Loopback, 0 );
    listener.Start();
    var port = ( (IPEndPoint) listener.LocalEndpoint ).Port;
    var settings = new CliSettings {
      ActiveEnvironment = "site-a", Environments = [new EnvironmentSetting( "site-a", $"http://127.0.0.1:{port}" )]
    };
    settings.Write( NullLogger.Instance, SettingsLocation );

    var started = DateTime.UtcNow;
    var (exitCode, output, error) = await DriftTestCli.InvokeAsync(
      "status",
      settingsLocation: SettingsLocation
    );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( error.ToString(), Is.Empty );
      Assert.That( output.ToString(), Does.Contain( "Status: Unavailable" ) );
      Assert.That( output.ToString(), Does.Contain( "Status probe timed out after 2 seconds." ) );
      Assert.That( DateTime.UtcNow - started, Is.LessThan( TimeSpan.FromSeconds( 5 ) ) );
    }
  }

  [Test]
  public async Task Status_CoordinatorWithEnrolledAgent_ShowsAgentState() {
    var controlPort = GetFreePort();
    var agentPort = GetFreePort();
    var dataLocation = new TemporaryCoordinatorDataLocation();
    using var agentCancellation = new CancellationTokenSource();
    var agentReady = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
    var agentTask = AgentHost.Run(
      agentPort,
      NullLogger.Instance,
      services => services.AddAgentHandlers(),
      agentCancellation.Token,
      agentReady
    );
    await agentReady.Task.WaitAsync( TimeSpan.FromSeconds( 10 ) );
    dataLocation.EnsureCreated();
    await File.WriteAllTextAsync(
      dataLocation.AgentEnrollmentFile,
      $$"""
        [
          {
            "id": "agent_one",
            "address": "http://127.0.0.1:{{agentPort}}",
            "enrolledAt": "2026-01-01T00:00:00+00:00"
          }
        ]
        """
    );

    await using var coordinator = CoordinatorHost.Build(
      controlPort,
      null,
      NullLogger.Instance,
      services => services.AddSingleton<ICoordinatorDataLocation>( dataLocation )
    );

    try {
      await coordinator.StartAsync();
      var settings = new CliSettings {
        ActiveEnvironment = "site-a",
        Environments = [new EnvironmentSetting( "site-a", $"http://127.0.0.1:{controlPort}" )]
      };
      settings.Write( NullLogger.Instance, SettingsLocation );

      var (exitCode, output, error) = await DriftTestCli.InvokeAsync(
        "status",
        settingsLocation: SettingsLocation
      );
      var rendered = output.ToString();

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
        Assert.That( error.ToString(), Is.Empty );
        Assert.That( rendered, Does.Contain( $"site-a @ http://127.0.0.1:{controlPort}" ) );
        Assert.That( rendered, Does.Contain( "Status: Ready" ) );
        Assert.That( rendered, Does.Contain( "Agents (1):" ) );
        Assert.That(
          rendered,
          Does.Contain( $"agent_one @ http://127.0.0.1:{agentPort}/ — Connected" )
        );
      }
    }
    finally {
      await coordinator.StopAsync();
      agentCancellation.Cancel();
      try {
        await agentTask;
      }
      catch ( OperationCanceledException ) when ( agentCancellation.IsCancellationRequested ) {
        // Expected when stopping the test agent.
      }

      Directory.Delete( dataLocation.Directory, true );
    }
  }

  private static ushort GetFreePort() {
    using var listener = new TcpListener( IPAddress.Loopback, 0 );
    listener.Start();
    return (ushort) ( (IPEndPoint) listener.LocalEndpoint ).Port;
  }
}