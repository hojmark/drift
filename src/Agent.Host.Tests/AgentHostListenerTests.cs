using System.Net;
using System.Net.Sockets;
using Drift.Common.IO;
using Drift.TestUtilities.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Agent.Host.Tests;

internal sealed class AgentHostListenerTests {
  [Test]
  public async Task Run_WithListenDisabled_DoesNotBindConfiguredPort() {
    var port = TcpUtils.GetFreePort();
    using var cancellation = new CancellationTokenSource();
    var ready = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
    var runTask = AgentHost.Run(
      new AgentOptions { Port = port, Listen = false },
      NullLogger.Instance,
      services => services.AddSingleton<IAgentDataLocation>( new TestAgentDataLocation() ),
      cancellation.Token,
      ready
    );

    try {
      await ready.Task.WaitAsync( TimeSpan.FromSeconds( 10 ), cancellation.Token );
      using var client = new TcpClient();
      Assert.ThrowsAsync<SocketException>( async () => await client.ConnectAsync( IPAddress.Loopback, port ) );
    }
    finally {
      await cancellation.CancelAsync();
      try {
        await runTask;
      }
      catch ( OperationCanceledException ) when ( cancellation.IsCancellationRequested ) {
        // Expected when stopping the host.
      }
    }
  }

  private sealed class TestAgentDataLocation : IAgentDataLocation {
    public string Directory => string.Empty;

    public void EnsureCreated() {
    }
  }
}