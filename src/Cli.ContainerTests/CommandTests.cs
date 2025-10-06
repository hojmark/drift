using System.Globalization;
using DotNet.Testcontainers.Builders;
using Drift.Cli.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Cli.ContainerTests;

internal sealed class CommandTests : DriftContainerImageFixture {
  [Test]
  public async Task ValidCommand_ReturnsSuccessExitCode() {
    // Arrange
    var container = new ContainerBuilder()
      .WithLogger( NullLogger.Instance )
      .WithImage( ImageTag )
      .WithCommand( "--help" )
      .Build();

    // Act
    await container.StartAsync().ConfigureAwait( false );

    // Assert
    var exitCode = await container.GetExitCodeAsync();
    var logs = await container.GetLogsAsync();

    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
    await Verify( logs ).ScrubInlineDateTimes( "yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture );
  }

  [Test]
  public async Task InvalidCommand_ReturnsErrorExitCode() {
    // Arrange
    var container = new ContainerBuilder()
      .WithLogger( NullLogger.Instance )
      .WithImage( ImageTag )
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