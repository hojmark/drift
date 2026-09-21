using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Drift.Agent.Host;
using Drift.Coordinator.Client;
using Drift.Coordinator.Client.Models;
using Drift.Coordinator.Host.Tests.Utils;
using Drift.Coordinator.Services.Agents;
using Drift.Coordinator.Services.State;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Scanning.Scanners.Factories;
using Drift.Scanning.Subnets.Interface;
using Drift.TestUtilities.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Coordinator.Host.Tests;

internal sealed class CoordinatorHostIntegrationTests {
  [Test]
  public async Task ServerStatus_IsAvailable() {
    var controlPort = GetFreePort();
    var agentPort = GetFreePort();
    await using var app = CoordinatorHost.Build( controlPort, agentPort, NullLogger.Instance );

    await app.StartAsync();
    try {
      using var client = new HttpClient { BaseAddress = new Uri( $"http://127.0.0.1:{controlPort}" ) };
      using var response = await client.GetAsync( "/api/v1/status" );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( response.StatusCode, Is.EqualTo( HttpStatusCode.OK ) );
        Assert.That( await response.Content.ReadAsStringAsync(), Is.EqualTo( "{\"status\":\"Ready\"}" ) );
      }
    }
    finally {
      await app.StopAsync();
    }
  }

  [Test]
  public async Task ApiDocsUi_IsAvailable() {
    var controlPort = GetFreePort();
    var agentPort = GetFreePort();
    await using var app = CoordinatorHost.Build( controlPort, agentPort, NullLogger.Instance );

    await app.StartAsync();
    try {
      using var client = new HttpClient { BaseAddress = new Uri( $"http://127.0.0.1:{controlPort}" ) };

      using var uiResponse = await client.GetAsync( "/api" );
      using var documentResponse = await client.GetAsync( "/api/v1/openapi.json" );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( uiResponse.StatusCode, Is.EqualTo( HttpStatusCode.OK ) );
        Assert.That( await uiResponse.Content.ReadAsStringAsync(), Contains.Substring( "Drift API" ) );
        Assert.That( documentResponse.StatusCode, Is.EqualTo( HttpStatusCode.OK ) );
      }
    }
    finally {
      await app.StopAsync();
    }
  }

  [Test]
  public async Task Enrollment_UndeclaredAgent_ReturnsUsefulError() {
    var controlPort = GetFreePort();
    var agentPort = GetFreePort();
    var dataLocation = new TemporaryCoordinatorDataLocation();
    await using var coordinator = CoordinatorHost.Build(
      controlPort,
      agentPort,
      NullLogger.Instance,
      services => services.AddSingleton<ICoordinatorDataLocation>( dataLocation )
    );

    try {
      await coordinator.StartAsync();
      using var client = new HttpClient { BaseAddress = new Uri( $"http://127.0.0.1:{controlPort}" ) };
      using var response = await client.PostAsync(
        "/api/v1/agents/enrollments",
        new StringContent( "{\"id\":\"agent_one\"}", Encoding.UTF8, "application/json" )
      );
      var body = await response.Content.ReadAsStringAsync();

      using var controlApiClient = ControlApiClient.Create( client.BaseAddress );
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
    var controlPort = GetFreePort();
    var coordinatorAgentPort = GetFreePort();
    var agentPort = GetFreePort();
    var dataLocation = new TemporaryCoordinatorDataLocation();
    var agentLogger = new StringLogger();
    var coordinatorLogger = new StringLogger();
    using var agentCancellation = new CancellationTokenSource();
    var agentReady = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
    var agentTask = AgentHost.Run(
      agentPort,
      agentLogger,
      services => {
        services.AddAgentHandlers();
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
      },
      agentCancellation.Token,
      agentReady
    );
    await agentReady.Task.WaitAsync( TimeSpan.FromSeconds( 10 ) );

    await using var coordinator = CoordinatorHost.Build(
      controlPort,
      coordinatorAgentPort,
      coordinatorLogger,
      services => services.AddSingleton<ICoordinatorDataLocation>( dataLocation )
    );

    try {
      await coordinator.StartAsync();
      using var client = new HttpClient { BaseAddress = new Uri( $"http://127.0.0.1:{controlPort}" ) };

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

      using var controlApiClient = ControlApiClient.Create( client.BaseAddress );
      var enrollmentResult = await controlApiClient.EnrollAgentAsync(
        AgentId.Parse( "agent_one", null ),
        CancellationToken.None
      );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( enrollmentResult.Id, Is.EqualTo( AgentId.Parse( "agent_one", null ) ) );
        Assert.That( enrollmentResult.ConnectionStatus, Is.EqualTo( AgentConnectionStatus.Connected ) );
      }

      await Verify( GetInformationLogs( agentLogger, agentPort ) )
        .UseFileName( "AgentHost_CliOutput" );
      await Verify(
          GetInformationLogs( coordinatorLogger, controlPort, coordinatorAgentPort, agentPort )
        )
        .UseFileName( "CoordinatorHost_CliOutput" );
    }
    finally {
      await coordinator.StopAsync();
      agentCancellation.Cancel();
      try {
        await agentTask;
      }
      catch ( OperationCanceledException ) when ( agentCancellation.IsCancellationRequested ) {
        Assert.That( agentCancellation.IsCancellationRequested, Is.True );
      }

      Directory.Delete( dataLocation.Directory, true );
    }
  }

  [Test]
  public async Task DistributedScan_ReconnectsEnrolledAgentAfterConnectionStateReset() {
    var controlPort = GetFreePort();
    var coordinatorAgentPort = GetFreePort();
    var agentPort = GetFreePort();
    var dataLocation = new TemporaryCoordinatorDataLocation();
    var agentLogger = new StringLogger();
    var coordinatorLogger = new StringLogger();
    using var agentCancellation = new CancellationTokenSource();
    var agentReady = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
    var agentTask = AgentHost.Run(
      agentPort,
      agentLogger,
      services => {
        services.AddAgentHandlers();
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
      },
      agentCancellation.Token,
      agentReady
    );
    await agentReady.Task.WaitAsync( TimeSpan.FromSeconds( 10 ) );

    await using var coordinator = CoordinatorHost.Build(
      controlPort,
      coordinatorAgentPort,
      coordinatorLogger,
      services => services.AddSingleton<ICoordinatorDataLocation>( dataLocation )
    );

    try {
      await coordinator.StartAsync();
      using var client = new HttpClient { BaseAddress = new Uri( $"http://127.0.0.1:{controlPort}" ) };

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
      var agentDirectory = coordinator.Services.GetRequiredService<IAgentDirectory>();
      agentDirectory.MarkUnavailable( AgentId.Parse( "agent_one", null ) );

      using var controlApiClient = ControlApiClient.Create( client.BaseAddress );
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

      var clientResult = await controlApiClient.GetResultAsync( scanId, CancellationToken.None );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( eventsResponse.StatusCode, Is.EqualTo( HttpStatusCode.OK ) );
        Assert.That( events, Contains.Substring( "event: scan" ) );
        Assert.That( events, Contains.Substring( "10.42.0.0/24" ) );
        Assert.That( resultResponse.StatusCode, Is.EqualTo( HttpStatusCode.OK ), resultJson );
        Assert.That( resultJson, Contains.Substring( "10.42.0.0/24" ) );
        Assert.That( resultJson, Contains.Substring( "\"status\":\"Success\"" ) );
        Assert.That( clientResults,
          Has.Some.Matches<NetworkScanResult>( result => result.Status == ScanResultStatus.Success ) );
        Assert.That( clientResult.Status, Is.EqualTo( ScanResultStatus.Success ) );
        Assert.That( clientResult.Subnets,
          Has.Some.Matches<SubnetScanResult>( subnet => subnet.CidrBlock == new CidrBlock( "10.42.0.0/24" ) ) );
      }

      await Verify( events.TrimEnd() )
        .UseFileName( "CoordinatorHost_DistributedScanEvents" )
        .ScrubLinesWithReplace( line => Regex.Replace(
          Regex.Replace( line, "[0-9a-f]{8}-[0-9a-f-]{27}", "<scan-id>" ),
          "\\d{4}-\\d{2}-\\d{2}T[^\\\"]+Z",
          "<time>"
        ) );
      await Verify( GetInformationLogs( agentLogger, agentPort ) )
        .UseFileName( "AgentHost_DistributedScanCliOutput" );
      await Verify( GetInformationLogs( coordinatorLogger, controlPort, coordinatorAgentPort, agentPort ) )
        .UseFileName( "CoordinatorHost_DistributedScanCliOutput" );
    }
    finally {
      await coordinator.StopAsync();
      agentCancellation.Cancel();
      try {
        await agentTask;
      }
      catch ( OperationCanceledException ) when ( agentCancellation.IsCancellationRequested ) {
        Assert.That( agentCancellation.IsCancellationRequested, Is.True );
      }

      Directory.Delete( dataLocation.Directory, true );
    }
  }

  private static ushort GetFreePort() {
    using var listener = new TcpListener( IPAddress.Loopback, 0 );
    listener.Start();
    return (ushort) ( (IPEndPoint) listener.LocalEndpoint ).Port;
  }

  private static string GetInformationLogs( StringLogger logger, params ushort[] ports ) {
    var output = logger.ToString()
      .Split( System.Environment.NewLine, StringSplitOptions.RemoveEmptyEntries )
      .Where( line =>
        line.StartsWith( "[INF]", StringComparison.Ordinal ) &&
        !line.Contains( "HTTP request", StringComparison.Ordinal ) &&
        !line.Contains( "Inbound stream", StringComparison.Ordinal ) &&
        !line.Contains( "Creating Inbound", StringComparison.Ordinal ) &&
        !line.Contains( "Stream #", StringComparison.Ordinal )
      );

    var result = string.Join( System.Environment.NewLine, output );
    foreach ( var port in ports ) {
      result = result.Replace( port.ToString(), "<port>" );
    }

    result = Regex.Replace( result, "[0-9a-f]{8}-[0-9a-f-]{27}", "<scan-id>" );

    return result + System.Environment.NewLine;
  }
}