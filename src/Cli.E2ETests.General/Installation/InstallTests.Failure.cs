using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

internal sealed partial class InstallTests {
  /// <summary>
  /// Returns the path to a patched copy of install.sh where the hard-coded <c>TARGET_ROOT=""</c>
  /// assignment (which fires whenever DRIFT_INSTALL_DIR is set) is replaced with
  /// <c>TARGET_ROOT="${TARGET_ROOT:-}"</c>, making it respect an env-var override.
  /// The caller is responsible for deleting the returned file.
  /// </summary>
  private static async Task<string> WritePatchedInstallScriptAsync() {
    var patched = ( await File.ReadAllTextAsync( InstallScript ) )
      .Replace( "TARGET_ROOT=\"\"", "TARGET_ROOT=\"${TARGET_ROOT:-}\"" );
    var path = Path.Combine( Path.GetTempPath(), "install-patched-" + Guid.NewGuid() + ".sh" );
    await File.WriteAllTextAsync( path, patched );
    return path;
  }

  [Test]
  public async Task InstallNonExistingVersion() {
    // Arrange / Act
    var installProcess = await new ToolWrapper( "bash" ).ExecuteAsync( InstallScript + " vBOGUS" );

    PrintInstallOutput( installProcess );

    // Assert
    Assert.That( installProcess.ExitCode, Is.EqualTo( ExitCodeFailure ) );
    await Verify( installProcess.StdOut ).UseTextForParameters( "INSTALL_OUTPUT" );
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
      Assert.That( installProcess.ExitCode, Is.EqualTo( ExitCodeFailure ) );
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

    var patchedScript = await WritePatchedInstallScriptAsync();

    try {
      // Act: TARGET_ROOT points at the conflicting plain file; DRIFT_INSTALL_DIR sets TARGET.
      // The patched script respects TARGET_ROOT from the environment instead of blanking it.
      var script = $"TARGET_ROOT={conflictingFile} DRIFT_INSTALL_DIR={installDir} bash {patchedScript}";
      var installProcess = await new ToolWrapper( "bash" ).ExecuteAsync( $"-c \"{script}\"" );

      PrintInstallOutput( installProcess );

      // Assert
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( installProcess.ExitCode, Is.EqualTo( ExitCodeFailure ) );
        Assert.That(
          installProcess.StdOut,
          Contains.Substring( "Refusing to create symlink" ),
          "Expected error message about refusing to create symlink"
        );
      }
    }
    finally {
      DeleteBestEffort( installDir, targetRootDir, patchedScript );
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

    var patchedScript = await WritePatchedInstallScriptAsync();

    try {
      // Act: TARGET_ROOT points at the conflicting symlink; DRIFT_INSTALL_DIR sets TARGET.
      // The patched script respects TARGET_ROOT from the environment instead of blanking it.
      var script = $"TARGET_ROOT={conflictingSymlink} DRIFT_INSTALL_DIR={installDir} bash {patchedScript}";
      var installProcess = await new ToolWrapper( "bash" ).ExecuteAsync( $"-c \"{script}\"" );

      PrintInstallOutput( installProcess );

      // Assert
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( installProcess.ExitCode, Is.EqualTo( ExitCodeFailure ) );
        Assert.That(
          installProcess.StdOut,
          Contains.Substring( "Refusing to update symlink" ),
          "Expected error message about refusing to update symlink"
        );
      }
    }
    finally {
      DeleteBestEffort( installDir, targetRootDir, patchedScript );
    }
  }

  /// <summary>
  /// install.sh should exit with error when required dependencies are missing, the user answers
  /// "y" to install them, but no package manager (apt/dnf/pacman) is available (line ~63).
  /// Uses an empty PATH so command -v curl/jq/tar and all package managers fail, then answers
  /// "y" to the install prompt.
  /// </summary>
  [Test]
  public async Task MissingDependenciesUserAnswersYesButNoPackageManager() {
    var installDir = Path.Combine( Path.GetTempPath(), "drift-install-nodeps-yes-" + Guid.NewGuid() );
    var fakeBinDir = Path.Combine( Path.GetTempPath(), "drift-fakebin-yes-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    Directory.CreateDirectory( fakeBinDir );

    try {
      // Empty PATH → command -v curl/jq/tar and apt/dnf/pacman all fail.
      // Pipe "y" to stdin so the script tries to install deps, then hits the "no package manager" error.
      var installProcess = await new ToolWrapper(
        "bash",
        new() { { "DRIFT_INSTALL_DIR", installDir }, { "PATH", fakeBinDir } }
      ).ExecuteAsync( $"-c \"echo y | /usr/bin/bash {InstallScript}\"" );

      PrintInstallOutput( installProcess );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That(
          installProcess.ExitCode,
          Is.EqualTo( ExitCodeFailure ),
          $"Expected exit code 1 when no package manager is available. Output: {installProcess.StdOut}"
        );
        Assert.That(
          installProcess.StdOut,
          Contains.Substring( "Could not be installed automatically" ),
          "Expected 'Could not be installed automatically' message in output"
        );
      }
    }
    finally {
      DeleteBestEffort( installDir, fakeBinDir );
    }
  }

  /// <summary>
  /// install.sh should exit with error when required dependencies are missing and the user
  /// declines to install them (line ~75).
  /// Uses an empty PATH so command -v curl/jq/tar all fail, then answers "n" to the prompt.
  /// </summary>
  [Test]
  public async Task MissingDependenciesWithNoPackageManager() {
    var installDir = Path.Combine( Path.GetTempPath(), "drift-install-nodeps-" + Guid.NewGuid() );
    var fakeBinDir = Path.Combine( Path.GetTempPath(), "drift-fakebin-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    Directory.CreateDirectory( fakeBinDir );

    try {
      // Empty PATH → command -v curl/jq/tar all fail → script prompts to install deps.
      // Pipe "n" to stdin so the script takes the "Installation cancelled" exit_with_error path.
      var installProcess = await new ToolWrapper(
        "bash",
        new() { { "DRIFT_INSTALL_DIR", installDir }, { "PATH", fakeBinDir } }
      ).ExecuteAsync( $"-c \"echo n | /usr/bin/bash {InstallScript}\"" );

      PrintInstallOutput( installProcess );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( installProcess.ExitCode, Is.EqualTo( ExitCodeFailure ) );
        Assert.That(
          installProcess.StdOut,
          Contains.Substring( "Installation cancelled" ),
          "Expected 'Installation cancelled' message in output"
        );
      }
    }
    finally {
      DeleteBestEffort( installDir, fakeBinDir );
    }
  }
}