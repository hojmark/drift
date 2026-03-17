using System.Text.RegularExpressions;
using Drift.Cli.Abstractions;
using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

internal sealed partial class InstallTests {
  // TODO split test into at least two parts
  [Test]
  public async Task InstallLatestVersion() {
    // Arrange: create a temporary install directory
    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    var driftBinary = Path.Combine( installDir, "drift" );

    try {
      // Act: run the install script
      var installProcess =
        await new ToolWrapper( "bash", new() { { "DRIFT_INSTALL_DIR", installDir } } )
          .ExecuteAsync( InstallScript );

      PrintInstallOutput( installProcess );

      // Assert: binary exists
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( installProcess.ExitCode, Is.EqualTo( ExitCodeSuccess ) );
        Assert.That( File.Exists( driftBinary ), Is.True, $"Drift binary not found at {driftBinary}" );
      }

      // Assert: install.sh output
      await Verify( installProcess.StdOut )
        .UseTextForParameters( "INSTALL_OUTPUT" )
        .ScrubLinesWithReplace( line =>
          Regex.Replace(
            Regex.Replace(
              line,
              @"drift_[\w\.\-]+_linux-x64\.tar\.gz",
              "drift_{VERSION}_linux-x64.tar.gz"
            ),
            @"Installed Drift CLI [\w\.\-]+ successfully!",
            "Installed Drift CLI {VERSION} successfully!"
          )
        );

      // Act: run drift
      var driftProcess = await new ToolWrapper( driftBinary ).ExecuteAsync( "--help" );

      Console.WriteLine( "------------------- drift output ----------------------" );

      await TestContext.Out.WriteLineAsync( driftProcess.StdOut );
      if ( !string.IsNullOrWhiteSpace( driftProcess.ErrOut ) ) {
        await TestContext.Out.WriteLineAsync( $"STDERR: {driftProcess.ErrOut}" );
      }

      Console.WriteLine( "------------------------------------------------------------" );

      // Assert: drift output
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( driftProcess.ExitCode, Is.EqualTo( ExitCodes.Success ) );
        Assert.That( driftProcess.StdOut, Is.Not.Null );
        Assert.That( driftProcess.StdOut, Contains.Substring( "-?, -h, --help  Show help and usage information" ) );
        Assert.That( driftProcess.ErrOut, Is.Empty );
      }
    }
    finally {
      DeleteBestEffort( installDir );
    }
  }

  /// <summary>
  /// install.sh should successfully install a specific version when a valid tag is provided.
  /// </summary>
  [Test]
  public async Task InstallSpecificVersion() {
    const string version = "v1.0.0-alpha.5";

    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-specific-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    var driftBinary = Path.Combine( installDir, "drift" );

    try {
      // Act
      var installProcess = await new ToolWrapper( "bash", new() { { "DRIFT_INSTALL_DIR", installDir } } )
        .ExecuteAsync( $"{InstallScript} {version}" );

      PrintInstallOutput( installProcess );

      // Assert: exit code and binary presence
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( installProcess.ExitCode, Is.EqualTo( ExitCodeSuccess ) );
        Assert.That( File.Exists( driftBinary ), Is.True, $"Drift binary not found at {driftBinary}" );
      }

      // Assert: output mentions the requested version
      Assert.That(
        installProcess.StdOut,
        Contains.Substring( $"Fetching version {version}" ),
        "Expected output to mention the requested version"
      );

      // Assert: install.sh output snapshot
      await Verify( installProcess.StdOut ).UseTextForParameters( "INSTALL_OUTPUT" );

      // Act: confirm the installed binary is executable
      var driftProcess = await new ToolWrapper( driftBinary ).ExecuteAsync( "--help" );

      // Assert: binary runs
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( driftProcess.ExitCode, Is.EqualTo( ExitCodes.Success ) );
        Assert.That( driftProcess.StdOut, Contains.Substring( "-?, -h, --help  Show help and usage information" ) );
        Assert.That( driftProcess.ErrOut, Is.Empty );
      }
    }
    finally {
      DeleteBestEffort( installDir );
    }
  }

  /// <summary>
  /// install.sh should succeed with --verbose and emit the verbose-mode banner plus set -x trace output.
  /// </summary>
  [Test]
  public async Task InstallWithVerbose() {
    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-verbose-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    var driftBinary = Path.Combine( installDir, "drift" );

    try {
      // Act
      var installProcess = await new ToolWrapper( "bash", new() { { "DRIFT_INSTALL_DIR", installDir } } )
        .ExecuteAsync( $"{InstallScript} --verbose" );

      PrintInstallOutput( installProcess );

      // Assert: install succeeded
      using ( Assert.EnterMultipleScope() ) {
        Assert.That(
          installProcess.ExitCode,
          Is.EqualTo( ExitCodeSuccess ),
          $"install.sh --verbose failed: {installProcess.ErrOut}"
        );
        Assert.That( File.Exists( driftBinary ), Is.True, $"Drift binary not found at {driftBinary}" );
      }

      // Assert: verbose mode was activated (banner line emitted by install.sh)
      Assert.That( installProcess.StdOut, Contains.Substring( "Verbose mode is ON" ) );

      // Assert: set -x trace output is present in stderr (bash writes xtrace to stderr)
      Assert.That(
        installProcess.ErrOut,
        Is.Not.Empty,
        "Expected set -x trace output on stderr when --verbose is used"
      );
    }
    finally {
      DeleteBestEffort( installDir );
    }
  }

  /// <summary>
  /// install.sh should succeed (exit 0) when TARGET_ROOT already holds a symlink that points
  /// to the correct binary (lines ~192-194). The script must not error or overwrite the symlink.
  /// </summary>
  [Test]
  public async Task SymlinkNotTouchedWhenAlreadyCorrect() {
    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-symlink-ok-" + Guid.NewGuid() );
    var targetRootDir = Path.Combine( tempDir, "drift-root-ok-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    Directory.CreateDirectory( targetRootDir );

    // Run the install once so the real binary lands in installDir.
    var firstInstall = await new ToolWrapper( "bash", new() { { "DRIFT_INSTALL_DIR", installDir } } )
      .ExecuteAsync( InstallScript );

    var driftBinary = Path.Combine( installDir, "drift" );
    var existingSymlink = Path.Combine( targetRootDir, "drift" );

    // Pre-create a symlink that already points at the installed binary.
    File.CreateSymbolicLink( existingSymlink, driftBinary );

    var patchedScript = await WritePatchedInstallScriptAsync();

    try {
      // Act: run install again with TARGET_ROOT already correct.
      var script = $"TARGET_ROOT={existingSymlink} DRIFT_INSTALL_DIR={installDir} /usr/bin/bash {patchedScript}";
      var installProcess = await new ToolWrapper( "bash" ).ExecuteAsync( $"-c \"{script}\"" );

      PrintInstallOutput( installProcess );

      // Assert: script exits successfully and symlink is still intact.
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( installProcess.ExitCode, Is.EqualTo( ExitCodeSuccess ) );
        Assert.That( File.Exists( existingSymlink ), Is.True, "Symlink should still exist after re-install" );

        var symlinkTarget = new FileInfo( existingSymlink ).LinkTarget;
        Assert.That( symlinkTarget, Is.EqualTo( driftBinary ), "Symlink should still point to the correct binary" );
      }

      _ = firstInstall; // suppress unused-variable warning; first install output not asserted
    }
    finally {
      DeleteBestEffort( installDir, targetRootDir, patchedScript );
    }
  }
}