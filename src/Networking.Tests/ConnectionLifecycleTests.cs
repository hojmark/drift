using System.Threading.Channels;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Tests.Helpers;

namespace Drift.Networking.Tests;

internal sealed class ConnectionLifecycleTests {
  [Test]
  public async Task CompletedConnectionIsReportedAsUnknownClosure() {
    await using var fixture = new MessageStreamManagerTestFixture();
    var closed = ObserveClosedConnections( fixture );
    var connection = fixture.OpenInbound( "agent_unknown" );

    await connection.CompleteAsync();

    Assert.That( await ReadAsync( closed ), Is.EqualTo( ConnectionCloseOrigin.Unknown ) );
  }

  [Test]
  public async Task DisposedManagerReportsConnectionClosureAsLocal() {
    await using var fixture = new MessageStreamManagerTestFixture();
    var closed = ObserveClosedConnections( fixture );
    fixture.OpenInbound( "agent_local" );

    await fixture.Manager.DisposeAsync();

    Assert.That( await ReadAsync( closed ), Is.EqualTo( ConnectionCloseOrigin.Local ) );
  }

  [Test]
  public async Task ReplacingAConnectionClosesOnlyTheReplacedConnectionLocally() {
    await using var fixture = new MessageStreamManagerTestFixture();
    var closed = ObserveClosedConnections( fixture );
    fixture.OpenInbound( "agent_replaced" );
    var replacement = fixture.OpenInbound( "agent_replaced" );

    Assert.That( await ReadAsync( closed ), Is.EqualTo( ConnectionCloseOrigin.Local ) );

    await replacement.CompleteAsync();

    Assert.That( await ReadAsync( closed ), Is.EqualTo( ConnectionCloseOrigin.Unknown ) );
  }

  [Test]
  public async Task ApplicationStoppingReportsConnectionAsLocal() {
    await using var fixture = new MessageStreamManagerTestFixture();
    var closed = ObserveClosedConnections( fixture );
    fixture.OpenInbound( "agent_stopping" );

    await fixture.StopAsync();

    Assert.That( await ReadAsync( closed ), Is.EqualTo( ConnectionCloseOrigin.Local ) );
  }

  private static ChannelReader<ConnectionCloseOrigin> ObserveClosedConnections(
    MessageStreamManagerTestFixture fixture
  ) {
    var closed = Channel.CreateUnbounded<ConnectionCloseOrigin>();
    fixture.Manager.ConnectionClosed += ( _, origin ) => closed.Writer.TryWrite( origin );
    return closed.Reader;
  }

  private static Task<ConnectionCloseOrigin> ReadAsync( ChannelReader<ConnectionCloseOrigin> closed ) {
    return closed.ReadAsync().AsTask().WaitAsync( TimeSpan.FromSeconds( 1 ) );
  }
}