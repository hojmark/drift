using System.Text.RegularExpressions;
using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils;
using Drift.TestUtilities;

namespace Drift.Cli.Tests.Commands;

// TODO picking a random port to remove NonParallelizable
[NonParallelizable]
internal sealed class ServerCommandTests {
  [CancelAfter( 3000 )]
  [Test]
  public async Task RespectsCancellationToken() {
    using var tcs = new CancellationTokenSource( TimeSpan.FromMilliseconds( 2000 ) );

    var (exitCode, output, _) = await DriftTestCli.InvokeAsync(
      "server start",
      cancellationToken: tcs.Token
    );

    Console.WriteLine( output );

    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
  }

  [Test]
  public async Task SuccessfulLifecycle() {
    using var tcs = new CancellationTokenSource();

    var runningCommand = await DriftTestCli.StartServerAsync(
      string.Empty,
      cancellationToken: tcs.Token
    );

    await tcs.CancelAsync();

    var (exitCode, output, error) = await runningCommand.Completion;

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      await Verify( output.ToString() )
        .ScrubVersion()
        // Connection state messages are not ordered deterministically (written by background service)
        .ScrubLinesWithReplace( line => Regex.Replace(
          line,
          @"Connection to agent .+ changed from Unknown to Unavailable: The messaging connection was closed\.\r?\n?",
          string.Empty
        ) );
      Assert.That( error.ToString(), Is.Empty );
    }
  }
}