using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

internal sealed partial class InstallPsTests {
  /// <summary>
  /// The install command shown in the README must work end-to-end.
  /// The command pipes install.ps1 directly from GitHub via irm and executes it via iex.
  /// Runs under both PowerShell 7 (pwsh) and Windows PowerShell 5.1 (powershell).
  /// </summary>
  [TestCase( "pwsh" )]
  [TestCase( "powershell" )]
  [Ignore( "No stable release with Windows assets exists yet. Re-enable once one is published." )]
  public async Task ReadmeInstallCommand( string shell ) {
    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-readme-ps-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    var driftBinary = Path.Combine( installDir, "drift.exe" );

    await AssertShellIsAvailable( shell );

    // The exact command from the README:
    const string readmeCommand =
      "irm https://raw.githubusercontent.com/hojmark/drift/refs/heads/main/install.ps1 | iex";

    try {
      // Act: run the README command with DRIFT_INSTALL_DIR set so the binary lands in a
      // temporary directory instead of the default user location.
      var installProcess =
        await new ToolWrapper( shell, new() { { "DRIFT_INSTALL_DIR", installDir } } )
          .ExecuteAsync( $"-NonInteractive -Command \"{readmeCommand}\"" );

      PrintInstallOutput( installProcess, shell );

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