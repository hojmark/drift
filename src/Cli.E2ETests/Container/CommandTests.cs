using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using Drift.Cli.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Cli.E2ETests.Container;

internal sealed class CommandTests : DriftImageFixture {
  [Test]
  public async Task ValidCommand_ReturnsSuccessExitCode() {
    // Arrange
    var container = new ContainerBuilder( DriftImage.ToString() )
      .WithLogger( NullLogger.Instance )
      .WithCommand( "--help" )
      .Build();

    // Act
    await container.StartAsync().ConfigureAwait( false );

    // Assert
    var exitCode = await container.GetExitCodeAsync();
    var logs = await container.GetLogsAsync();

    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
    await Verify( logs ).ScrubLinesWithReplace( line =>
      Regex.Replace(
        line,
        @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.*?(?= )",
        "<time>"
      )
    );
  }

  [Test]
  public async Task InvalidCommand_ReturnsErrorExitCode() {
    // Arrange
    var container = new ContainerBuilder( DriftImage.ToString() )
      .WithLogger( NullLogger.Instance )
      .WithCommand( "bogus" )
      .Build();

    // Act
    await container.StartAsync().ConfigureAwait( false );

    // Assert
    var exitCode = await container.GetExitCodeAsync();
    var logs = await container.GetLogsAsync();

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( logs.Stderr, Does.Contain( "Unrecognized command or argument 'bogus'" ) );
      Assert.That( exitCode, Is.EqualTo( ExitCodes.SystemCommandLineDefaultError ) );
    }
  }
}