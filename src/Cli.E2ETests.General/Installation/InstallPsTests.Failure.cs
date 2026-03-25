using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

internal sealed partial class InstallPsTests {
  [TestCase( "pwsh" )]
  [TestCase( "powershell" )]
  public async Task InstallNonExistingVersion( string shell ) {
    await AssertShellIsAvailable( shell );

    // Arrange / Act
    var installProcess =
      await new ToolWrapper( shell ).ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\" vBOGUS" );

    PrintInstallOutput( installProcess, shell );

    // Assert
    Assert.That( installProcess.ExitCode, Is.EqualTo( ScriptExitCodeFailure ) );
    await Verify( installProcess.StdOut ).UseTextForParameters( $"{shell}_INSTALL_OUTPUT" );
  }
}