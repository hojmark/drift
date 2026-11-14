using System.Net;
using System.Net.Sockets;
using Drift.Cli.Abstractions;
using Drift.Cli.Settings.Tests;
using Drift.Cli.Settings.V1_preview;
using Drift.Cli.Settings.V1_preview.Environments;
using Drift.Cli.Tests.Utils;
using Drift.Cli.Tests.Utils.Coordinator;
using Drift.Common.IO;
using Drift.Coordinator.Client;
using Drift.Coordinator.Host;
using Drift.TestUtilities.Hosts;
using Drift.TestUtilities.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Cli.Tests.Commands;

internal sealed class StatusCommandTests {
  private IDriftSettingsLocation SettingsLocation {
    get;
  } = new TemporaryDriftSettingsLocation();

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
      Assert.That( output.ToString(), Does.Contain( "Settings directory:" ) );
      Assert.That( output.ToString(), Does.Contain( "Local addresses:" ) );
      Assert.That( output.ToString(), Does.Contain( "local @ local" ) );
      Assert.That( output.ToString(), Does.Contain( "Status: Local scanning" ) );
      Assert.That( output.ToString(), Does.Contain( "Spec: Not found" ) );
    }
  }

  [Test]
  [NonParallelizable]
  public async Task Status_LocalSpec_ShowsLinkedPathAndResourceCounts() {
    var originalDirectory = Directory.GetCurrentDirectory();
    var specDirectory = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString( "N" ) );
    Directory.CreateDirectory( specDirectory );

    try {
      Directory.SetCurrentDirectory( specDirectory );
      await File.WriteAllTextAsync(
        "drift.spec.yaml",
        """
        version: v1-preview
        network:
          subnets:
            - address: 10.42.0.0/24
          devices: []
        """
      );

      var (exitCode, output, error) = await DriftTestCli.InvokeAsync(
        "status",
        settingsLocation: SettingsLocation
      );
      var rendered = string.Join( " ",
        output.ToString()!.Split( (char[]?) null, StringSplitOptions.RemoveEmptyEntries ) );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
        Assert.That( error.ToString(), Is.Empty );
        Assert.That( rendered, Does.Contain( $"Spec: {Path.Combine( specDirectory, "drift.spec.yaml" )}" ) );
        Assert.That( rendered, Does.Contain( "1 subnet, 0 devices" ) );
      }
    }
    finally {
      Directory.SetCurrentDirectory( originalDirectory );
      Directory.Delete( specDirectory, true );
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

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( error.ToString(), Is.Empty );
      await Verify(
        string.Join(
          Environment.NewLine,
          output.ToString()?.Split( Environment.NewLine ).Take( 5 ) ?? throw new Exception( "Failed to parse output" )
        )
      ).UniqueForOSPlatform();
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
    var controlPort = TcpUtils.GetFreePort();
    var agentPort = TcpUtils.GetFreePort();
    var dataLocation = new TemporaryCoordinatorDataLocation();
    await using var agent = await RunningAgentHost.StartAsync(
      agentPort,
      NullLogger.Instance,
      _ => {
      }
    );
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
    await using var coordinator = await RunningCoordinatorHost.StartAsync(
      new CoordinatorOptions { Port = controlPort, ListenForAgents = false },
      NullLogger.Instance,
      services => services.AddSingleton<ICoordinatorDataLocation>( dataLocation )
    );

    try {
      var settings = new CliSettings {
        ActiveEnvironment = "site-a",
        Environments = [new EnvironmentSetting( "site-a", coordinator.Address.ToString() )]
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
        Assert.That( rendered, Does.Contain( $"site-a @ {coordinator.Address}" ) );
        Assert.That( rendered, Does.Contain( "Status: Spec missing" ) );
        Assert.That( rendered, Does.Not.Contain( "Status: Ready" ) );
        Assert.That( rendered, Does.Contain( "Spec: Not uploaded" ) );
        Assert.That( rendered, Does.Contain( "Agents (1):" ) );
        Assert.That(
          rendered,
          Does.Contain( $"agent_one @ http://127.0.0.1:{agentPort}/ — Connected" )
        );
      }
    }
    finally {
      await coordinator.StopAsync();
      Directory.Delete( dataLocation.Directory, true );
    }
  }

  [Test]
  public async Task Status_CoordinatorWithUploadedSpec_ShowsResourceCounts() {
    var controlPort = TcpUtils.GetFreePort();
    var dataLocation = new TemporaryCoordinatorDataLocation();
    await using var coordinator = await RunningCoordinatorHost.StartAsync(
      new CoordinatorOptions { Port = controlPort, ListenForAgents = false },
      NullLogger.Instance,
      services => services.AddSingleton<ICoordinatorDataLocation>( dataLocation )
    );

    try {
      using ( var client = ControlApiClient.Create( coordinator.Address ) ) {
        await client.ApplySpecAsync(
          """
          version: v1-preview
          network:
            subnets:
              - address: 10.42.0.0/24
            devices: []
          """,
          CancellationToken.None
        );
      }

      var settings = new CliSettings {
        ActiveEnvironment = "site-a",
        Environments = [new EnvironmentSetting( "site-a", coordinator.Address.ToString() )]
      };
      settings.Write( NullLogger.Instance, SettingsLocation );

      var (exitCode, output, error) = await DriftTestCli.InvokeAsync(
        "status",
        settingsLocation: SettingsLocation
      );
      var rendered = string.Join( " ",
        output.ToString()!.Split( (char[]?) null, StringSplitOptions.RemoveEmptyEntries ) );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
        Assert.That( error.ToString(), Is.Empty );
        Assert.That( rendered, Does.Contain( "Status: Ready" ) );
        Assert.That( rendered, Does.Contain( "Spec: Uploaded — 1 subnet, 0 devices" ) );
      }
    }
    finally {
      await coordinator.StopAsync();
      if ( Directory.Exists( dataLocation.Directory ) ) {
        Directory.Delete( dataLocation.Directory, true );
      }
    }
  }
}