using Drift.Agent.Host;
using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils;
using Drift.Common.IO;
using Drift.TestUtilities;
using Drift.TestUtilities.Hosts;
using Drift.TestUtilities.IO;
using Environment = Drift.TestUtilities.Environment;

namespace Drift.Cli.Tests.Commands;

internal sealed class AgentCommandTests {
  [CancelAfter( 3000 )]
  [Test]
  public async Task RespectsCancellationToken() {
    using var tcs = new CancellationTokenSource( TimeSpan.FromMilliseconds( 2000 ) );

    var (exitCode, output, _) = await DriftTestCli.InvokeAsync(
      "agent start --port " + TcpUtils.GetFreePort(),
      cancellationToken: tcs.Token
    );

    Console.WriteLine( output );

    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
  }

  [Test]
  public async Task SuccessfulLifecycle() {
    using var tcs = new CancellationTokenSource();

    var dataLocation = new TemporaryDriftDataLocation();

    var runningCommand = await DriftTestCli.StartAgentAsync(
      "--port " + TcpUtils.GetFreePort(),
      cancellationToken: tcs.Token,
      dataLocation: dataLocation
    );

    await tcs.CancelAsync();

    var (exitCode, output, error) = await runningCommand.Completion;

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      await Verify( output.ToString() ).ScrubVersion().ScrubPort();
      Assert.That( error.ToString(), Is.Empty );
      // The agent uses the CLI's data location
      Assert.That( Path.Combine( dataLocation.Directory, "agent" ), Does.Exist );
    }
  }

  [Test]
  [NonParallelizable] // Binds the default port
  public async Task SuccessfulLifecycle_WithoutPortOption_UsesDefaultPort() {
    using var tcs = new CancellationTokenSource();

    var runningCommand = await DriftTestCli.StartAgentAsync( string.Empty, cancellationToken: tcs.Token );

    await tcs.CancelAsync();

    var (exitCode, output, error) = await runningCommand.Completion;

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( output.ToString(), Does.Contain( $"Port: {AgentOptions.DefaultPort} (gRPC)" ) );
      Assert.That( output.ToString(), Does.Contain( "Agent stopped" ) );
      Assert.That( error.ToString(), Is.Empty );
    }
  }

  [Test]
  [NonParallelizable] // Mutates XDG_DATA_HOME
  public async Task SuccessfulLifecycle_WithDefaultDataLocation_UsesXdgDataHome() {
    Environment.SkipIfNot( Platform.Linux ); // XDG_DATA_HOME can't redirect the default data location on Windows

    using var xdgDataHome = new TemporaryXdgDataHome();
    using var tcs = new CancellationTokenSource();

    var runningCommand = await DriftTestCli.StartAgentAsync(
      "--port " + TcpUtils.GetFreePort(),
      cancellationToken: tcs.Token,
      dataLocation: new DefaultDriftDataLocation()
    );

    await tcs.CancelAsync();

    var (exitCode, _, error) = await runningCommand.Completion;

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( Path.Combine( xdgDataHome.DriftDataDirectory, "agent" ), Does.Exist );
      Assert.That( error.ToString(), Is.Empty );
    }
  }
}