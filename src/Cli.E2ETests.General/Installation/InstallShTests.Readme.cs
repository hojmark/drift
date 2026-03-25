using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

internal sealed partial class InstallShTests {
  /// <summary>
  /// The install command shown in the README must work end-to-end.
  /// The command pipes install.sh directly from GitHub and runs it via bash.
  /// </summary>
  [Test]
  public async Task ReadmeInstallCommand() {
    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-readme-sh-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    var driftBinary = Path.Combine( installDir, "drift" );

    // The exact command from the README:
    const string readmeCommand =
      "curl -sSL https://raw.githubusercontent.com/hojmark/drift/refs/heads/main/install.sh | bash";

    try {
      // Act: run the README command via bash -c, with DRIFT_INSTALL_DIR set so the binary
      // lands in a temporary directory instead of the system path.
      var installProcess =
        await new ToolWrapper( "bash", new() { { "DRIFT_INSTALL_DIR", installDir } } )
          .ExecuteAsync( $"-c \"{readmeCommand}\"" );

      PrintInstallOutput( installProcess );

      // Assert: install succeeded and binary is present
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( installProcess.ExitCode, Is.EqualTo( ExitCodeSuccess ) );
        Assert.That( File.Exists( driftBinary ), Is.True, $"Drift binary not found at {driftBinary}" );
      }

      // Assert: installed binary is functional
      var driftProcess = await new ToolWrapper( driftBinary ).ExecuteAsync( "--version" );
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( driftProcess.ExitCode, Is.EqualTo( ExitCodeSuccess ) );
        Assert.That( driftProcess.StdOut, Is.Not.Empty );
      }
    }
    finally {
      DeleteBestEffort( installDir );
    }
  }
}