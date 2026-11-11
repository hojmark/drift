using System.Net;
using System.Text;
using System.Text.Json;
using Drift.TestUtilities.Hosts;
using Drift.TestUtilities.IO;

// ReSharper disable MethodSupportsCancellation

namespace Drift.Coordinator.Host.Tests;

/// <summary>
/// Untrusted input reaching the logs must not be able to forge log entries (CodeQL cs/log-forging).
/// </summary>
internal sealed class LogForgingTests {
  private const string ForgedEntry = "[INF] forged entry";

  [TestCase( "/api/v1/agents/enrollments" )]
  [TestCase( "/api/v1/agents/enrollment-requests" )]
  public async Task Enrollment_AgentIdWithLineBreak_IsRejectedAndNotLogged( string endpoint ) {
    var logger = new StringLogger();
    await using var coordinator = await RunningCoordinatorHost.StartAsync(
      new CoordinatorConfiguration { Port = TcpUtils.GetFreePort(), AgentPort = TcpUtils.GetFreePort() },
      logger
    );
    using var client = coordinator.CreateHttpClient();

    using var response = await client.PostAsync(
      endpoint,
      new StringContent(
        JsonSerializer.Serialize( new { id = "agent_x\n" + ForgedEntry } ),
        Encoding.UTF8,
        "application/json"
      )
    );
    var body = await response.Content.ReadAsStringAsync();

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( response.StatusCode, Is.EqualTo( HttpStatusCode.BadRequest ) );
      Assert.That( body, Contains.Substring( "Agent ID must be" ) );
      Assert.That( body, Does.Not.Contain( "forged" ) );
      Assert.That( logger.ToString(), Does.Contain( "invalid agent ID" ) );
      Assert.That( logger.ToString(), Does.Not.Contain( "forged" ) );
    }
  }

  [Test]
  public async Task RequestPath_WithEncodedLineBreak_IsLoggedEncoded() {
    var logger = new StringLogger();
    await using var coordinator = await RunningCoordinatorHost.StartAsync(
      new CoordinatorConfiguration { Port = TcpUtils.GetFreePort(), AgentPort = TcpUtils.GetFreePort() },
      logger
    );
    using var client = coordinator.CreateHttpClient();

    using var response = await client.GetAsync( "/api/v1/nonexistent%0A" + Uri.EscapeDataString( ForgedEntry ) );

    var lines = logger.ToString().Split( '\n' );
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( response.StatusCode, Is.EqualTo( HttpStatusCode.NotFound ) );
      Assert.That( logger.ToString(), Does.Contain( "/api/v1/nonexistent%0A" ) );
      Assert.That( lines, Has.None.StartsWith( ForgedEntry ) );
    }
  }
}