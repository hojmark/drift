using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Coordinator.Host.Tests;

internal sealed class CoordinatorHostIntegrationTests {
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

  private static ushort GetFreePort() {
    using var listener = new TcpListener( IPAddress.Loopback, 0 );
    listener.Start();
    return (ushort) ( (IPEndPoint) listener.LocalEndpoint ).Port;
  }
}