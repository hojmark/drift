using Drift.Cli.Abstractions;
using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

internal sealed partial class InstallShTests {
  private const string ShReadmeCommand =
    "curl -sSL https://raw.githubusercontent.com/hojmark/drift/refs/heads/main/install.sh | bash";

  [Test]
  public async Task ReadmeInstallCommandIsPresentInReadme() {
    var readmePath = Path.Combine( Path.GetDirectoryName( InstallScript )!, "README.md" );
    var readmeContent = await File.ReadAllTextAsync( readmePath );
    Assert.That(
      readmeContent,
      Contains.Substring( ShReadmeCommand ),
      $"Expected README.md to contain the install command: {ShReadmeCommand}"
    );
  }

  [Test]
  public async Task ReadmeInstallCommand() {
    // Arrange
    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-readme-sh-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    var driftBinary = Path.Combine( installDir, "drift" );

    try {
      // Act
      var installProcess =
        await new ToolWrapper( "bash", new() { { "DRIFT_INSTALL_DIR", installDir } } )
          .ExecuteAsync( $"-c \"{ShReadmeCommand}\"" );

      PrintInstallOutput( installProcess );

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