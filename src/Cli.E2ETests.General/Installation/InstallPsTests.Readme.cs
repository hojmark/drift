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
  public async Task ReadmeInstallCommand( string shell ) {
    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-readme-ps-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    var driftBinary = Path.Combine( installDir, "drift.exe" );

    await AssertShellIsAvailable( shell );

    try {
      // Act: run the README command with DRIFT_INSTALL_DIR set so the binary lands in a
      // temporary directory instead of the default user location.
      var installProcess =
        await new ToolWrapper( shell, new() { { "DRIFT_INSTALL_DIR", installDir } } )
          .ExecuteAsync( $"-NonInteractive -Command \"{PsReadmeCommand}\"" );

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