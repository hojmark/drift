using System.Text.RegularExpressions;
using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils;
using Drift.Common.IO;
using Drift.TestUtilities;
using Drift.TestUtilities.Hosts;
using Drift.TestUtilities.IO;
using Environment = Drift.TestUtilities.Environment;

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
    var dataLocation = new TemporaryDriftDataLocation();

    var runningCommand = await DriftTestCli.StartServerAsync(
      $"--port {port} --port-agent {agentPort}",
      cancellationToken: tcs.Token,
      dataLocation: dataLocation
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
        .ScrubLines( line => ConnectionStateLine().IsMatch( line ) );
      Assert.That( error.ToString(), Is.Empty );
      // The server uses the CLI's data location
      Assert.That( Path.Combine( dataLocation.Directory, "coordinator" ), Does.Exist );
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

  [Test]
  [NonParallelizable] // Mutates XDG_DATA_HOME
  public async Task SuccessfulLifecycle_WithDefaultDataLocation_UsesXdgDataHome() {
    Environment.SkipIfNot( Platform.Linux );

    using var xdgDataHome = new TemporaryXdgDataHome();
    using var tcs = new CancellationTokenSource();

    var runningCommand = await DriftTestCli.StartServerAsync(
      $"--port {TcpUtils.GetFreePort()} --port-agent {TcpUtils.GetFreePort()}",
      cancellationToken: tcs.Token,
      dataLocation: new DefaultDriftDataLocation()
    );

    await tcs.CancelAsync();

    var (exitCode, _, error) = await runningCommand.Completion;

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( Path.Combine( xdgDataHome.DriftDataDirectory, "coordinator" ), Does.Exist );
      Assert.That( error.ToString(), Is.Empty );
    }
  }

  [GeneratedRegex(
    @"^Connection to agent .+ changed from Unknown to Unavailable: The messaging connection was closed\.$" )]
  private static partial Regex ConnectionStateLine();
}