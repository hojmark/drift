using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

internal sealed partial class InstallPsTests {
  [TestCase( "pwsh" )]
  [TestCase( "powershell" )]
  public async Task InstallNonExistingVersion( string shell ) {
    await AssertShellIsAvailable( shell );

    // Arrange / Act
    var installProcess = await new ToolWrapper( shell )
      .ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\" vBOGUS" );

    PrintInstallOutput( installProcess, shell );

    // Assert
    Assert.That( installProcess.ExitCode, Is.EqualTo( ScriptExitCodeFailure ) );
    await Verify( installProcess.StdOut ).UseTextForParameters( $"{shell}_INSTALL_OUTPUT" );
  }

  [TestCase( "pwsh" )]
  [TestCase( "powershell" )]
  public async Task InstallWithInvalidGitHubTokenFails( string shell ) {
    const string invalidToken = "invalid-token-for-testing";

    await AssertShellIsAvailable( shell );

    var installProcess = await new ToolWrapper(
      shell,
      new() { { "GITHUB_TOKEN", invalidToken } }
    ).ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\" v1.0.0-alpha.7" );

    PrintInstallOutput( installProcess, shell );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( installProcess.ExitCode, Is.EqualTo( ScriptExitCodeFailure ) );
      Assert.That( installProcess.StdOut, Does.Not.Contain( invalidToken ) );
      Assert.That( installProcess.ErrOut, Does.Not.Contain( invalidToken ) );
    }

    await Verify( installProcess.StdOut ).UseTextForParameters( $"{shell}_INSTALL_OUTPUT" );
  }
}