using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

internal sealed partial class InstallTests {
  [Test]
  public async Task InstallNonExistingVersion() {
    // Arrange / Act
    var installProcess = await new ToolWrapper( "bash" ).ExecuteAsync( InstallScript + " vBOGUS" );

    PrintInstallOutput( installProcess );

    // Assert
    Assert.That(
      installProcess.ExitCode,
      Is.EqualTo( 1 ),
      $"install.sh unexpectedly didn't fail: {installProcess.StdOut}"
    );
    await Verify( installProcess.StdOut )
      .UseTextForParameters( "INSTALL_OUTPUT" );
  }

  /// <summary>
  /// install.sh should exit with error when an unknown argument is passed (line ~108).
  /// </summary>
  [Test]
  public async Task UnknownArgument() {
    // Arrange / Act
    var installProcess = await new ToolWrapper( "bash" ).ExecuteAsync( InstallScript + " --unknown-flag" );

    PrintInstallOutput( installProcess );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( installProcess.ExitCode, Is.EqualTo( 1 ), "Expected exit code 1 for unknown argument" );
      Assert.That( installProcess.StdOut, Contains.Substring( "Unknown argument: --unknown-flag" ) );
    }
  }

 /// <summary>
  /// install.sh should refuse to create a symlink at TARGET_ROOT when a regular (non-symlink)
  /// file already exists there (line ~187).
  /// </summary>
  [Test]
  public async Task SymlinkRefusedWhenRegularFileExistsAtTargetRoot() {
    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-symlink-" + Guid.NewGuid() );
    var targetRootDir = Path.Combine( tempDir, "drift-root-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    Directory.CreateDirectory( targetRootDir );

    // Place a plain file where the symlink would be created
    var conflictingFile = Path.Combine( targetRootDir, "drift" );
    await File.WriteAllTextAsync( conflictingFile, "not a symlink" );

    try {
      // Act: override TARGET_ROOT via env-var injection so we can point it at our temp dirs
      var script = $"TARGET_ROOT={conflictingFile} DRIFT_INSTALL_DIR={installDir} bash {InstallScript}";
      var installProcess = await new ToolWrapper( "bash" ).ExecuteAsync( $"-c \"{script}\"" );

      PrintInstallOutput( installProcess );

      // Assert
      using ( Assert.EnterMultipleScope() ) {
        Assert.That(
          installProcess.ExitCode,
          Is.EqualTo( 1 ),
          "Expected exit code 1 when a regular file blocks symlink creation"
        );
        Assert.That(
          installProcess.StdOut,
          Contains.Substring( "Refusing to create symlink" ),
          "Expected error message about refusing to create symlink"
        );
      }
    }
    finally {
      try {
        Directory.Delete( installDir, true );
      }
      catch {
        // best-effort cleanup
      }

      try {
        Directory.Delete( targetRootDir, true );
      }
      catch {
        // best-effort cleanup
      }
    }
  }

  /// <summary>
  /// install.sh should refuse to update an existing symlink at TARGET_ROOT when it points
  /// to a different binary than the one just installed (line ~196).
  /// </summary>
  [Test]
  public async Task SymlinkRefusedWhenExistingSymlinkPointsElsewhere() {
    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-symlink2-" + Guid.NewGuid() );
    var targetRootDir = Path.Combine( tempDir, "drift-root2-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    Directory.CreateDirectory( targetRootDir );

    // Create a symlink pointing to a different (unrelated) file
    var someOtherBinary = Path.Combine( targetRootDir, "other" );
    await File.WriteAllTextAsync( someOtherBinary, "other binary" );
    var conflictingSymlink = Path.Combine( targetRootDir, "drift" );
    File.CreateSymbolicLink( conflictingSymlink, someOtherBinary );

    try {
      // Act: inject TARGET_ROOT override so it points at the conflicting symlink
      var script = $"TARGET_ROOT={conflictingSymlink} DRIFT_INSTALL_DIR={installDir} bash {InstallScript}";
      var installProcess = await new ToolWrapper( "bash" ).ExecuteAsync( $"-c \"{script}\"" );

      PrintInstallOutput( installProcess );

      // Assert
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( installProcess.ExitCode, Is.EqualTo( 1 ), "Expected exit code 1 when symlink points elsewhere" );
        Assert.That(
          installProcess.StdOut,
          Contains.Substring( "Refusing to update symlink" ),
          "Expected error message about refusing to update symlink"
        );
      }
    }
    finally {
      try {
        Directory.Delete( installDir, true );
      }
      catch {
        // best-effort cleanup
      }

      try {
        Directory.Delete( targetRootDir, true );
      }
      catch {
        // best-effort cleanup
      }
    }
  }

  /// <summary>
  /// install.sh should exit with error when required dependencies are missing and no supported
  /// package manager is available to install them (line ~63).
  /// This test strips known package managers from PATH and removes the tools to force the error path.
  /// </summary>
  [Test]
  public async Task MissingDependenciesWithNoPackageManager() {
    var installDir = Path.Combine( Path.GetTempPath(), "drift-install-nodeps-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );

    try {
      // Restrict PATH to core shell tools only, dropping curl/jq and all package managers
      // (apt, dnf, pacman). The script detects missing deps, finds no package manager → exit 1.
      // On non-interactive stdin, set -euo pipefail also causes an early exit if read -p fails.
      var installProcess = await new ToolWrapper(
        "bash",
        new() { { "DRIFT_INSTALL_DIR", installDir }, { "PATH", "/usr/bin:/bin" } }
      ).ExecuteAsync( InstallScript );

      PrintInstallOutput( installProcess );

      Assert.That(
        installProcess.ExitCode,
        Is.Not.Zero,
        $"Expected non-zero exit when required dependencies are unavailable. Output: {installProcess.StdOut}"
      );
    }
    finally {
      try {
        Directory.Delete( installDir, true );
      }
      catch {
        // best-effort cleanup
      }
    }
  }
}