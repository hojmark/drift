using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils;

namespace Drift.Cli.Tests.Commands;

internal sealed class AgentCommandTests {
  [CancelAfter( 3000 )]
  [Test]
  public async Task RespectsCancellationToken() {
    using var tcs = new CancellationTokenSource( TimeSpan.FromMilliseconds( 2000 ) );

    var (exitCode, output, _) = await DriftTestCli.InvokeAsync(
      "agent start",
      cancellationToken: tcs.Token
    );

    Console.WriteLine( output );

    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
  }

  [Test]
  public async Task SuccessfulLifecycle() {
    using var tcs = new CancellationTokenSource();

    var runningCommand = await DriftTestCli.StartAgentAsync(
      string.Empty,
      cancellationToken: tcs.Token
    );

    await tcs.CancelAsync();

    var (exitCode, output, error) = await runningCommand.Completion;

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      await Verify( output.ToString() );
      Assert.That( error.ToString(), Is.Empty );
    }
  }
}