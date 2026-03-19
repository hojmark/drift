using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

internal sealed partial class InstallPsTests {
  /// <summary>
  /// install.ps1 should exit with an error when a tag that does not exist on GitHub is requested.
  /// </summary>
  [Test]
  public async Task InstallNonExistingVersion() {
    // Arrange / Act
    var installProcess = await new ToolWrapper( "pwsh" ).ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\" vBOGUS" );

    PrintInstallOutput( installProcess );

    // Assert
    Assert.That( installProcess.ExitCode, Is.EqualTo( ExitCodeFailure ) );
    await Verify( installProcess.StdOut ).UseTextForParameters( "INSTALL_OUTPUT" );
  }
}