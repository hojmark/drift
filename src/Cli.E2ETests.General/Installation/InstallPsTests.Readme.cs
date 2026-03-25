using Drift.Cli.Abstractions;
using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

internal sealed partial class InstallPsTests {
  private const string PsReadmeCommand =
    "irm https://raw.githubusercontent.com/hojmark/drift/refs/heads/main/install.ps1 | iex";

  [Test]
  public async Task ReadmeInstallCommandIsPresentInReadme() {
    var readmePath = Path.Combine( Path.GetDirectoryName( InstallScript )!, "README.md" );
    var readmeContent = await File.ReadAllTextAsync( readmePath );
    Assert.That(
      readmeContent,
      Contains.Substring( PsReadmeCommand ),
      $"Expected README.md to contain the install command: {PsReadmeCommand}"
    );
  }

  [TestCase( "pwsh" )]
  [TestCase( "powershell" )]
  [Ignore( "No stable release with Windows assets exists yet. Re-enable once one is published." )]
  public async Task ReadmeInstallCommand( string shell ) {
    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-readme-ps-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    var driftBinary = Path.Combine( installDir, "drift.exe" );

    await AssertShellIsAvailable( shell );

    try {
      // Act
      var installProcess =
        await new ToolWrapper( shell, new() { { "DRIFT_INSTALL_DIR", installDir } } )
          .ExecuteAsync( $"-NonInteractive -Command \"{PsReadmeCommand}\"" );

      PrintInstallOutput( installProcess, shell );

      // Assert
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( installProcess.ExitCode, Is.EqualTo( ScriptExitCodeSuccess ) );
        Assert.That( File.Exists( driftBinary ), Is.True, $"Drift binary not found at {driftBinary}" );
      }

      var driftProcess = await new ToolWrapper( driftBinary ).ExecuteAsync( "--version" );
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( driftProcess.ExitCode, Is.EqualTo( ExitCodes.Success ) );
        Assert.That( driftProcess.StdOut, Is.Not.Empty );
      }
    }
    finally {
      DeleteBestEffort( installDir );
    }
  }
}