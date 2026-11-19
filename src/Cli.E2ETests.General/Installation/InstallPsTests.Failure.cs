using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

internal sealed partial class InstallPsTests {
  [Test]
  public async Task InstallNonExistingVersion() {
    await AssertPowerShellIsAvailable();

    // Arrange / Act
    var installProcess = await new ToolWrapper( "pwsh" )
      .ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\" vBOGUS" );

    PrintInstallOutput( installProcess );

    // Assert
    Assert.That( installProcess.ExitCode, Is.EqualTo( ScriptExitCodeFailure ) );
    await Verify( installProcess.StdOut );
  }

  [Test]
  public async Task InstallWithInvalidGitHubTokenFails() {
    const string invalidToken = "invalid-token-for-testing";

    await AssertPowerShellIsAvailable();

    var installProcess = await new ToolWrapper(
      "pwsh",
      new() { { "GITHUB_TOKEN", invalidToken } }
    ).ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\" v1.0.0-alpha.7" );

    PrintInstallOutput( installProcess );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( installProcess.ExitCode, Is.EqualTo( ScriptExitCodeFailure ) );
      Assert.That( installProcess.StdOut, Does.Not.Contain( invalidToken ) );
      Assert.That( installProcess.ErrOut, Does.Not.Contain( invalidToken ) );
    }

    await Verify( installProcess.StdOut );
  }
}