using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Drift.Common.IO;
using Drift.Coordinator.Client;
using Drift.Coordinator.Client.Models;
using Drift.Coordinator.Host.Tests.Utils;
using Drift.Coordinator.Services.Agents;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Scanning.Scanners.Factories;
using Drift.Scanning.Subnets.Interface;
using Drift.TestUtilities;
using Drift.TestUtilities.Hosts;
using Drift.TestUtilities.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable MethodSupportsCancellation

namespace Drift.Coordinator.Host.Tests;

internal sealed class CoordinatorHostIntegrationTests {
  [Test]
  public async Task ServerStatus_IsAvailable() {
    await using var app = await RunningCoordinatorHost.StartAsync(
      new CoordinatorConfiguration { Port = TcpUtils.GetFreePort(), AgentPort = TcpUtils.GetFreePort() },
      NullLogger.Instance
    );
    using var client = app.CreateHttpClient();
    using var response = await client.GetAsync( "/api/v1/status" );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( response.StatusCode, Is.EqualTo( HttpStatusCode.OK ) );
      Assert.That( await response.Content.ReadAsStringAsync(), Is.EqualTo( "{\"status\":\"Ready\"}" ) );
    }
  }

  [Test]
  public async Task ApiDocsUi_IsAvailable() {
    await using var app = await RunningCoordinatorHost.StartAsync(
      new CoordinatorConfiguration { Port = TcpUtils.GetFreePort(), AgentPort = TcpUtils.GetFreePort() },
      NullLogger.Instance
    );
    using var client = app.CreateHttpClient();

    using var uiResponse = await client.GetAsync( "/api" );
    using var documentResponse = await client.GetAsync( "/api/v1/openapi.json" );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( uiResponse.StatusCode, Is.EqualTo( HttpStatusCode.OK ) );
      Assert.That( await uiResponse.Content.ReadAsStringAsync(), Contains.Substring( "Drift API" ) );
      Assert.That( documentResponse.StatusCode, Is.EqualTo( HttpStatusCode.OK ) );
    }
  }

  [Test]
  public async Task Enrollment_UndeclaredAgent_ReturnsUsefulError() {
    var controlPort = TcpUtils.GetFreePort();
    var agentPort = TcpUtils.GetFreePort();
    var dataLocation = new TemporaryCoordinatorDataLocation();
    await using var coordinator = await RunningCoordinatorHost.StartAsync(
      new CoordinatorConfiguration { Port = controlPort, AgentPort = agentPort },
      NullLogger.Instance,
      services => services.AddSingleton<ICoordinatorDataLocation>( dataLocation )
    );
    try {
      using var client = coordinator.CreateHttpClient();
      using var response = await client.PostAsync(
        "/api/v1/agents/enrollments",
        new StringContent( "{\"id\":\"agent_one\"}", Encoding.UTF8, "application/json" )
      );
      var body = await response.Content.ReadAsStringAsync();

      using var controlApiClient = ControlApiClient.Create( coordinator.Address );
      var exception = Assert.ThrowsAsync<HttpRequestException>( async () =>
        await controlApiClient.EnrollAgentAsync(
          AgentId.Parse( "agent_one", null ),
          CancellationToken.None
        )
      );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( response.StatusCode, Is.EqualTo( HttpStatusCode.BadRequest ) );
        Assert.That( body, Contains.Substring( "Agent enrollment rejected" ) );
        Assert.That( body, Contains.Substring( "Agent 'agent_one' is not declared in the coordinator spec." ) );
        Assert.That(
          exception!.Message,
          Is.EqualTo(
            "Coordinator request failed with 400 (Bad Request). Agent enrollment rejected: Agent 'agent_one' is not declared in the coordinator spec."
          )
        );
      }
    }
    finally {
      await coordinator.StopAsync();
      Directory.Delete( dataLocation.Directory, true );
    }
  }

  [Test]
  public async Task Enrollment_ConnectsToRealAgentAndReportsConnectedStatus() {
    var controlPort = TcpUtils.GetFreePort();
    var coordinatorAgentPort = TcpUtils.GetFreePort();
    var agentPort = TcpUtils.GetFreePort();
    var dataLocation = new TemporaryCoordinatorDataLocation();
    var agentLogger = new StringLogger();
    var coordinatorLogger = new StringLogger();
    var agentDirectory = new InMemoryAgentDirectory( [] );
    await using var agent = await RunningAgentHost.StartAsync(
      agentPort,
      agentLogger,
      services => {
        services.AddSingleton<IInterfaceSubnetProvider>(
          new PredefinedInterfaceSubnetProvider(
            [
              new NetworkInterface {
                Description = "test",
                OperationalStatus = System.Net.NetworkInformation.OperationalStatus.Up,
                UnicastAddress = new CidrBlock( "10.42.0.0/24" )
              }
            ]
          )
        );
      }
    );

    await using var coordinator = await RunningCoordinatorHost.StartAsync(
      new CoordinatorConfiguration {
        Port = controlPort, AgentPort = coordinatorAgentPort, EnableRequestLogging = false
      },
      coordinatorLogger,
      services => {
        services.AddSingleton<ICoordinatorDataLocation>( dataLocation );
        services.AddSingleton<IAgentDirectory>( agentDirectory );
      }
    );

    try {
      using var client = coordinator.CreateHttpClient();

      dataLocation.EnsureCreated();
      await File.WriteAllTextAsync(
        dataLocation.SpecFile,
        $"""
         version: v1-preview
         network:
           subnets: []
           devices: []
         agents:
           - id: agent_one
             address: http://127.0.0.1:{agentPort}
         """
      );

      using var enrollmentRequestResponse = await client.PostAsync(
        "/api/v1/agents/enrollment-requests",
        new StringContent( "{\"id\":\"agent_one\"}", Encoding.UTF8, "application/json" )
      );
      var enrollmentRequestJson = await enrollmentRequestResponse.Content.ReadAsStringAsync();
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( enrollmentRequestResponse.StatusCode, Is.EqualTo( HttpStatusCode.OK ), enrollmentRequestJson );
        Assert.That( enrollmentRequestJson, Contains.Substring( "\"connectionStatus\":\"Unknown\"" ) );
      }

      using var controlApiClient = ControlApiClient.Create( coordinator.Address );
      var enrollmentResult = await controlApiClient.EnrollAgentAsync(
        AgentId.Parse( "agent_one", null ),
        CancellationToken.None
      );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( enrollmentResult.Id, Is.EqualTo( AgentId.Parse( "agent_one", null ) ) );
        Assert.That( enrollmentResult.ConnectionStatus, Is.EqualTo( AgentConnectionStatus.Connected ) );
      }

      await Verify( GetInformationLogs( agentLogger, agentPort ) )
        .UseFileName( "AgentHost_CliOutput" )
        .ScrubVersion();
      await Verify(
          GetInformationLogs( coordinatorLogger, controlPort, coordinatorAgentPort, agentPort )
        )
        .UseFileName( "CoordinatorHost_CliOutput" )
        .ScrubVersion();
    }
    finally {
      await coordinator.StopAsync();
      await agent.StopAsync();
      Directory.Delete( dataLocation.Directory, true );
    }
  }

  [Test]
  public async Task DistributedScan_ReconnectsEnrolledAgentAfterConnectionStateReset() {
    var controlPort = TcpUtils.GetFreePort();
    var coordinatorAgentPort = TcpUtils.GetFreePort();
    var agentPort = TcpUtils.GetFreePort();
    var dataLocation = new TemporaryCoordinatorDataLocation();
    var agentLogger = new StringLogger();
    var coordinatorLogger = new StringLogger();
    var agentDirectory = new InMemoryAgentDirectory( [] );
    await using var agent = await RunningAgentHost.StartAsync(
      agentPort,
      agentLogger,
      services => {
        services.AddSingleton<IInterfaceSubnetProvider>(
          new PredefinedInterfaceSubnetProvider(
            [
              new NetworkInterface {
                Description = "test",
                OperationalStatus = System.Net.NetworkInformation.OperationalStatus.Up,
                UnicastAddress = new CidrBlock( "10.42.0.0/24" )
              }
            ]
          )
        );
        services.AddSingleton<ISubnetScannerFactory, TestSubnetScannerFactory>();
      }
    );

    await using var coordinator = await RunningCoordinatorHost.StartAsync(
      new CoordinatorConfiguration {
        Port = controlPort, AgentPort = coordinatorAgentPort, EnableRequestLogging = false
      },
      coordinatorLogger,
      services => {
        services.AddSingleton<ICoordinatorDataLocation>( dataLocation );
        services.AddSingleton<IAgentDirectory>( agentDirectory );
      }
    );

    try {
      using var client = coordinator.CreateHttpClient();

      using var specResponse = await client.PutAsync(
        "/api/v1/spec",
        new StringContent(
          $"""
           version: v1-preview
           network:
             subnets:
               - address: 10.42.0.0/24
             devices: []
           agents:
             - id: agent_one
               address: http://127.0.0.1:{agentPort}
           """,
          Encoding.UTF8,
          "text/plain"
        )
      );
      Assert.That( specResponse.StatusCode, Is.EqualTo( HttpStatusCode.NoContent ) );

      using var enrollmentResponse = await client.PostAsync(
        "/api/v1/agents/enrollments",
        new StringContent( "{\"id\":\"agent_one\"}", Encoding.UTF8, "application/json" )
      );
      Assert.That( enrollmentResponse.StatusCode, Is.EqualTo( HttpStatusCode.OK ) );
      agentDirectory.MarkUnavailable( AgentId.Parse( "agent_one", null ) );

      using var controlApiClient = ControlApiClient.Create( coordinator.Address );
      var scanId = await controlApiClient.StartScanAsync( 50, CancellationToken.None );

      using var eventsResponse = await client.GetAsync(
        $"/api/v1/scans/{scanId}/events",
        HttpCompletionOption.ResponseHeadersRead
      );
      var events = await eventsResponse.Content.ReadAsStringAsync();
      using var resultResponse = await client.GetAsync( $"/api/v1/scans/{scanId}/results" );
      var resultJson = await resultResponse.Content.ReadAsStringAsync();
      var clientResults = new List<NetworkScanResult>();
      await foreach ( var result in controlApiClient.WatchScanAsync( scanId, CancellationToken.None ) ) {
        clientResults.Add( result );
      }

      var clientResult = await controlApiClient.GetScanAsync( scanId, CancellationToken.None );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( eventsResponse.StatusCode, Is.EqualTo( HttpStatusCode.OK ) );
        Assert.That( events, Contains.Substring( "event: scan" ) );
        Assert.That( events, Contains.Substring( "10.42.0.0/24" ) );
        Assert.That( resultResponse.StatusCode, Is.EqualTo( HttpStatusCode.OK ), resultJson );
        Assert.That( resultJson, Contains.Substring( "10.42.0.0/24" ) );
        Assert.That( resultJson, Contains.Substring( "\"status\":\"Success\"" ) );
        Assert.That(
          clientResults,
          Has.Some.Matches<NetworkScanResult>( result => result.Status == ScanResultStatus.Success )
        );
        Assert.That( clientResult.Status, Is.EqualTo( ScanResultStatus.Success ) );
        Assert.That(
          clientResult.Subnets,
          Has.Some.Matches<SubnetScanResult>( subnet => subnet.CidrBlock == new CidrBlock( "10.42.0.0/24" ) )
        );
      }

      await Verify( events.TrimEnd() )
        .UseFileName( "CoordinatorHost_DistributedScanEvents" )
        .ScrubLinesWithReplace( line => Regex.Replace(
          Regex.Replace( line, "[0-9a-f]{8}-[0-9a-f-]{27}", "<scan-id>" ),
          "\\d{4}-\\d{2}-\\d{2}T[^\\\"]+Z",
          "<time>"
        ) );
      await Verify( GetInformationLogs( agentLogger, agentPort ) )
        .UseFileName( "AgentHost_DistributedScanCliOutput" )
        .ScrubVersion();
      await Verify( GetInformationLogs( coordinatorLogger, controlPort, coordinatorAgentPort, agentPort ) )
        .UseFileName( "CoordinatorHost_DistributedScanCliOutput" )
        .ScrubVersion();
    }
    finally {
      await coordinator.StopAsync();
      await agent.StopAsync();
      Directory.Delete( dataLocation.Directory, true );
    }
  }

  private static string GetInformationLogs( StringLogger logger, params ushort[] ports ) {
    var output = logger.ToString().Split( System.Environment.NewLine );

    var result = string.Join( System.Environment.NewLine, output );
    foreach ( var port in ports ) {
      result = result.Replace( port.ToString(), "<port>" );
    }

    result = Regex.Replace( result, "[0-9a-f]{8}-[0-9a-f-]{27}", "<scan-id>" );

    return result;
  }
}