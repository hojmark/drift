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

    Console.WriteLine( $"Created temp install directory: {installDir}" );
    Console.WriteLine();

    try {
      // Act: run the install script
      var installProcess =
        await new ToolWrapper( "bash", new() { { "DRIFT_INSTALL_DIR", installDir } } )
          .ExecuteAsync( InstallScript );

      PrintInstallOutput( installProcess );

      // Assert: binary exists
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( installProcess.ExitCode, Is.Zero, $"install.sh failed: {installProcess.ErrOut}" );
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
      try {
        Directory.Delete( installDir, true );
        Console.WriteLine( $"Deleted temp install directory: {installDir}" );
      }
      catch ( Exception ex ) {
        Console.WriteLine( $"Warning: Failed to delete temp dir {installDir}: {ex.Message}" );
      }
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
        Assert.That( installProcess.ExitCode, Is.Zero, $"install.sh failed: {installProcess.ErrOut}" );
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
      try {
        Directory.Delete( installDir, true );
      }
      catch ( Exception ex ) {
        Console.WriteLine( $"Warning: Failed to delete temp dir {installDir}: {ex.Message}" );
      }
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
        Assert.That( installProcess.ExitCode, Is.Zero, $"install.sh --verbose failed: {installProcess.ErrOut}" );
        Assert.That( File.Exists( driftBinary ), Is.True, $"Drift binary not found at {driftBinary}" );
      }

      // Assert: verbose mode was activated (banner line emitted by install.sh)
      Assert.That(
        installProcess.StdOut,
        Contains.Substring( "Verbose mode is ON" ),
        "Expected verbose banner in output"
      );

      // Assert: set -x trace output is present in stderr (bash writes xtrace to stderr)
      Assert.That(
        installProcess.ErrOut,
        Is.Not.Empty,
        "Expected set -x trace output on stderr when --verbose is used"
      );
    }
    finally {
      try {
        Directory.Delete( installDir, true );
      }
      catch ( Exception ex ) {
        Console.WriteLine( $"Warning: Failed to delete temp dir {installDir}: {ex.Message}" );
      }
    }
  }
}