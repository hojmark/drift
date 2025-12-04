using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils;

namespace Drift.Cli.Tests.Commands;

internal sealed class AgentCommandTests {
  [CancelAfter( 10000 )]
  [Test]
  public async Task RespectsCancellationToken() {
    using var tcs = new CancellationTokenSource( TimeSpan.FromSeconds( 5 ) );

    var (exitCode, output, _) = await DriftTestCli.InvokeFromTestAsync(
      "agent start --adoptable",
      cancellationToken: tcs.Token
    );

    Console.WriteLine( output );

    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
  }
}