using System.Text.RegularExpressions;
using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils;
using Drift.TestUtilities;
using Drift.TestUtilities.IO;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class ServerCommandTests {
  [CancelAfter( 3000 )]
  [Test]
  public async Task RespectsCancellationToken() {
    using var tcs = new CancellationTokenSource( TimeSpan.FromMilliseconds( 2000 ) );

    var (exitCode, output, _) = await DriftTestCli.InvokeAsync(
      $"server start --port {TcpUtils.GetFreePort()} --port-agent {TcpUtils.GetFreePort()}",
      cancellationToken: tcs.Token
    );

    Console.WriteLine( output );

    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
  }

  [Test]
  public async Task SuccessfulLifecycle() {
    using var tcs = new CancellationTokenSource();
    var port = TcpUtils.GetFreePort();
    var agentPort = TcpUtils.GetFreePort();

    var runningCommand = await DriftTestCli.StartServerAsync(
      $"--port {port} --port-agent {agentPort}",
      cancellationToken: tcs.Token
    );

    await tcs.CancelAsync();

    var (exitCode, output, error) = await runningCommand.Completion;

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      await Verify( output.ToString() )
        .ScrubVersion()
        .ScrubLinesWithReplace( line => Regex.Replace( line, $@"\b{port}\b", "<port>" ) )
        .ScrubLinesWithReplace( line => Regex.Replace( line, $@"\b{agentPort}\b", "<agent-port>" ) )
        // Connection state messages are not ordered deterministically (written by background service)
        .ScrubLines( line => ConnectionStateLine().IsMatch( line )
        );
      Assert.That( error.ToString(), Is.Empty );
    }
  }

  [Test]
  [NonParallelizable] // Binds the default ports
  public async Task SuccessfulLifecycle_WithoutPortOptions_UsesDefaultPorts() {
    using var tcs = new CancellationTokenSource();

    var runningCommand = await DriftTestCli.StartServerAsync( string.Empty, cancellationToken: tcs.Token );

    await tcs.CancelAsync();

    var (exitCode, output, error) = await runningCommand.Completion;

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( output.ToString(), Does.Contain( $"Control API port: {Ports.AgentDefault - 5} (HTTP)" ) );
      Assert.That( output.ToString(), Does.Contain( $"Agent port: {Ports.AgentDefault} (gRPC)" ) );
      Assert.That( output.ToString(), Does.Contain( "Server stopped" ) );
      Assert.That( error.ToString(), Is.Empty );
    }
  }

  [GeneratedRegex(
    @"^Connection to agent .+ changed from Unknown to Unavailable: The messaging connection was closed\.$" )]
  private static partial Regex ConnectionStateLine();
}