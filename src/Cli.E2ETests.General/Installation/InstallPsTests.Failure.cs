using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

internal sealed partial class InstallPsTests {
  /// <summary>
  /// install.ps1 should exit with an error when a tag that does not exist on GitHub is requested.
  /// Runs under both PowerShell 7 (pwsh) and Windows PowerShell 5.1 (powershell).
  /// </summary>
  [TestCase( "pwsh" )]
  [TestCase( "powershell" )]
  public async Task InstallNonExistingVersion( string shell ) {
    // Arrange / Act
    var installProcess = await new ToolWrapper( shell ).ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\" vBOGUS" );

    PrintInstallOutput( installProcess );

    // Assert
    Assert.That( installProcess.ExitCode, Is.EqualTo( ExitCodeFailure ) );
    await Verify( installProcess.StdOut ).UseTextForParameters( "INSTALL_OUTPUT" );
  }
}