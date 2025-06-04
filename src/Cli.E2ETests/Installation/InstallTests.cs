using System.Text.RegularExpressions;
using Drift.Cli.Abstractions;
using Drift.Utils;

namespace Drift.Cli.E2ETests.Installation;

public class InstallTests {
  //TODO split test into at least two parts
  [Test]
  public async Task InstallLatestVersion() {
    // Arrange: find the install.sh script
    var repoRoot = TestContext.CurrentContext.TestDirectory;
    while ( !File.Exists( Path.Combine( repoRoot, "install.sh" ) ) && repoRoot != "/" ) {
      repoRoot = Path.GetDirectoryName( repoRoot )!;
    }

    var installScript = Path.Combine( repoRoot, "install.sh" );
    Assert.That( File.Exists( installScript ), $"Could not find install.sh at repo root: {installScript}" );

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
          .ExecuteAsync( installScript );

      Assert.That( installProcess.ExitCode, Is.EqualTo( 0 ) );

      Console.WriteLine( "------------------- install.sh output ----------------------" );

      await TestContext.Out.WriteLineAsync( installProcess.StdOut );
      if ( !string.IsNullOrWhiteSpace( installProcess.ErrOut ) )
        await TestContext.Out.WriteLineAsync( $"STDERR: {installProcess.ErrOut}" );

      Console.WriteLine( "------------------------------------------------------------" );

      // Act: run drift
      var driftProcess = await new ToolWrapper( driftBinary ).ExecuteAsync( "--help" );

      Console.WriteLine( "------------------- drift output ----------------------" );

      await TestContext.Out.WriteLineAsync( driftProcess.StdOut );
      if ( !string.IsNullOrWhiteSpace( driftProcess.ErrOut ) )
        await TestContext.Out.WriteLineAsync( $"STDERR: {driftProcess.ErrOut}" );

      Console.WriteLine( "------------------------------------------------------------" );

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

      // Assert: binary exists
      Assert.Multiple( () => {
        Assert.That( installProcess.ExitCode, Is.EqualTo( 0 ), $"install.sh failed: {installProcess.ErrOut}" );
        Assert.That( File.Exists( driftBinary ), Is.True, $"Drift binary not found at {driftBinary}" );
      } );

      // Assert: drift output
      Assert.Multiple( () => {
        Assert.That( driftProcess.ExitCode, Is.EqualTo( ExitCodes.Success ) );
        Assert.That( driftProcess.StdOut, Is.Not.Null );
        Assert.That( driftProcess.StdOut, Contains.Substring( "-?, -h, --help  Show help and usage information" ) );
        Assert.That( driftProcess.ErrOut, Is.Empty );
      } );
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

  //[Test]
  public Task InstallSpecificVersion() {
    return Task.CompletedTask;
    // TODO
  }

  //[Test]
  public Task InstallNonExistingVersion() {
    return Task.CompletedTask;
    // TODO
  }

  //[Test]
  public Task InstallWithVerbose() {
    return Task.CompletedTask;
    // TODO
  }
}