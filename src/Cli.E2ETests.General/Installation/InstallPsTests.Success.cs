using System.Text.RegularExpressions;
using Drift.Cli.Abstractions;
using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

internal sealed partial class InstallPsTests {
  // TODO split test into at least two parts
  [TestCase( "pwsh" )]
  [TestCase( "powershell" )]
  public async Task InstallLatestVersion( string shell ) {
    // Arrange: create a temporary install directory
    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-ps-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    var driftBinary = Path.Combine( installDir, "drift.exe" );

    try {
      // Act: run the install script
      var installProcess = await new ToolWrapper( shell, new() { { "DRIFT_INSTALL_DIR", installDir } } )
        .ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\"" );

      PrintInstallOutput( installProcess, shell );

      // Assert: binary exists
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( installProcess.ExitCode, Is.EqualTo( ScriptExitCodeSuccess ) );
        Assert.That( File.Exists( driftBinary ), Is.True, $"Drift binary not found at {driftBinary}" );
      }

      // Assert: install.ps1 output
      await Verify( installProcess.StdOut )
        .UseTextForParameters( $"{shell}_INSTALL_OUTPUT" )
        .ScrubLinesWithReplace( line =>
          Regex.Replace(
            Regex.Replace(
              Regex.Replace(
                Regex.Replace(
                  line,
                  @"drift_[\w\.\-]+_win-x64\.zip",
                  "drift_{VERSION}_win-x64.zip"
                ),
                @"Installed Drift CLI [\w\.\-]+ successfully!",
                "Installed Drift CLI {VERSION} successfully!"
              ),
              @">> Installing to .+\.\.\.",
              ">> Installing to {INSTALL_DIR}..."
            ),
            @"   Adding .+ to user PATH\.\.\.",
            "   Adding {INSTALL_DIR} to user PATH..."
          )
        );

      // Act: run drift
      var driftProcess = await new ToolWrapper( driftBinary ).ExecuteAsync( "--version" );

      Console.WriteLine( "------------------- drift output ----------------------" );
      await TestContext.Out.WriteLineAsync( driftProcess.StdOut );
      if ( !string.IsNullOrWhiteSpace( driftProcess.ErrOut ) ) {
        await TestContext.Out.WriteLineAsync( $"STDERR: {driftProcess.ErrOut}" );
      }

      Console.WriteLine( "-------------------------------------------------------" );

      // Assert: drift output
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( driftProcess.ExitCode, Is.EqualTo( ExitCodes.Success ) );
        Assert.That( driftProcess.StdOut, Is.Not.Empty );
        Assert.That( driftProcess.ErrOut, Is.Empty );
      }
    }
    finally {
      DeleteBestEffort( installDir );
    }
  }

  [TestCase( "pwsh" )]
  [TestCase( "powershell" )]
  public async Task InstallSpecificVersion( string shell ) {
    // TODO Update when a newer stable release with Windows assets is available.
    const string version = "v0.0.0-windows.11.20260323204120";

    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-ps-specific-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    var driftBinary = Path.Combine( installDir, "drift.exe" );

    await AssertShellIsAvailable( shell );

    try {
      // Act
      var installProcess =
        await new ToolWrapper( shell, new() { { "DRIFT_INSTALL_DIR", installDir } } )
          .ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\" {version}" );

      PrintInstallOutput( installProcess, shell );

      // Assert: exit code and binary presence
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( installProcess.ExitCode, Is.EqualTo( ScriptExitCodeSuccess ) );
        Assert.That( File.Exists( driftBinary ), Is.True, $"Drift binary not found at {driftBinary}" );
      }

      // Assert: output mentions the requested version
      Assert.That(
        installProcess.StdOut,
        Contains.Substring( $"Fetching version {version}" ),
        "Expected output to mention the requested version"
      );

      // Assert: install.ps1 output snapshot
      await Verify( installProcess.StdOut )
        .UseTextForParameters( $"{shell}_INSTALL_OUTPUT" )
        .ScrubLinesWithReplace( line => {
            var stripped = Regex.Replace( line, @">> Installing to .+\.\.\.", ">> Installing to {INSTALL_DIR}..." );
            stripped = Regex.Replace(
              stripped,
              @"   Adding .+ to user PATH\.\.\.",
              "   Adding {INSTALL_DIR} to user PATH..."
            );
            return stripped;
          }
        );

      // Act: confirm the installed binary reports the expected version
      var driftProcess = await new ToolWrapper( driftBinary ).ExecuteAsync( "--version" );

      // Assert: binary runs and reports the requested version
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( driftProcess.ExitCode, Is.EqualTo( ExitCodes.Success ) );
        Assert.That(
          driftProcess.StdOut,
          Contains.Substring( "0.0.0-windows.11.20260323204120" ),
          $"Expected --version to report the installed version, got: {driftProcess.StdOut}"
        );
        Assert.That( driftProcess.ErrOut, Is.Empty );
      }
    }
    finally {
      DeleteBestEffort( installDir );
    }
  }

  [TestCase( "pwsh" )]
  [TestCase( "powershell" )]
  public async Task UpgradeFromPreviousVersion( string shell ) {
    // previousVersion = last prerelease Windows build; latest stable = v1.0.0-alpha.7
    const string previousVersion = "v0.0.0-windows.11.20260323204120";

    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-ps-upgrade-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );
    var driftBinary = Path.Combine( installDir, "drift.exe" );

    await AssertShellIsAvailable( shell );

    try {
      // Act: install the specific previous version
      var firstInstall =
        await new ToolWrapper( shell, new() { { "DRIFT_INSTALL_DIR", installDir } } )
          .ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\" {previousVersion}" );

      PrintInstallOutput( firstInstall, shell );

      // Assert: first install succeeded
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( firstInstall.ExitCode, Is.EqualTo( ScriptExitCodeSuccess ) );
        Assert.That( File.Exists( driftBinary ), Is.True, $"Drift binary not found at {driftBinary}" );
      }

      // Assert: binary reports the previous version
      var versionAfterFirst = await new ToolWrapper( driftBinary ).ExecuteAsync( "--version" );
      Assert.That(
        versionAfterFirst.StdOut,
        Contains.Substring( "0.0.0-windows.11" ),
        $"Expected --version to report previous version after first install, got: {versionAfterFirst.StdOut}"
      );

      // Act: upgrade by installing the latest (no version arg), reusing the same install directory
      var secondInstall =
        await new ToolWrapper( shell, new() { { "DRIFT_INSTALL_DIR", installDir } } )
          .ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\"" );

      PrintInstallOutput( secondInstall, shell );

      // Assert: upgrade succeeded
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( secondInstall.ExitCode, Is.EqualTo( ScriptExitCodeSuccess ) );
        Assert.That( File.Exists( driftBinary ), Is.True, $"Drift binary not found at {driftBinary}" );
      }

      // Assert: binary now reports a version newer than the one we started with
      var versionAfterSecond = await new ToolWrapper( driftBinary ).ExecuteAsync( "--version" );
      Assert.That(
        versionAfterSecond.StdOut,
        Is.Not.EqualTo( versionAfterFirst.StdOut ),
        $"Expected --version to change after upgrading to latest, but it stayed: {versionAfterSecond.StdOut}"
      );
    }
    finally {
      DeleteBestEffort( installDir );
    }
  }

  [TestCase( "pwsh" )]
  [TestCase( "powershell" )]
  public async Task InstallCreatesDirectoryIfMissing( string shell ) {
    var tempDir = Path.GetTempPath();
    // Point to a directory that does not exist yet
    var installDir = Path.Combine( tempDir, "drift-install-ps-newdir-" + Guid.NewGuid(), "nested", "drift" );
    var driftBinary = Path.Combine( installDir, "drift.exe" );

    try {
      Assert.That( Directory.Exists( installDir ), Is.False, "Pre-condition: install dir must not exist" );

      // Act
      var installProcess =
        await new ToolWrapper( shell, new() { { "DRIFT_INSTALL_DIR", installDir } } )
          .ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\"" );

      PrintInstallOutput( installProcess, shell );

      // Assert
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( installProcess.ExitCode, Is.EqualTo( ScriptExitCodeSuccess ) );
        Assert.That( Directory.Exists( installDir ), Is.True, "Install dir should have been created" );
        Assert.That( File.Exists( driftBinary ), Is.True, $"Drift binary not found at {driftBinary}" );
      }
    }
    finally {
      // Walk up to the root of the temp dir tree we created
      DeleteBestEffort( Path.GetDirectoryName( Path.GetDirectoryName( installDir ) )! );
    }
  }

  [TestCase( "pwsh" )]
  [TestCase( "powershell" )]
  public async Task InstallAddsToUserPath( string shell ) {
    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-ps-path-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );

    // Capture User PATH before install so we can restore it afterwards
    var originalPath = Environment.GetEnvironmentVariable( "PATH", EnvironmentVariableTarget.User ) ?? string.Empty;

    // Remove installDir from User PATH if it is somehow already there (defensive pre-condition)
    var cleanedEntries = originalPath.Split( ';' )
      .Where( e => !e.Equals( installDir, StringComparison.OrdinalIgnoreCase ) );
    Environment.SetEnvironmentVariable( "PATH", string.Join( ";", cleanedEntries ), EnvironmentVariableTarget.User );

    try {
      // Act
      var installProcess =
        await new ToolWrapper( shell, new() { { "DRIFT_INSTALL_DIR", installDir } } )
          .ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\"" );

      PrintInstallOutput( installProcess, shell );

      Assert.That( installProcess.ExitCode, Is.EqualTo( ScriptExitCodeSuccess ) );

      // Read User PATH after install (from the registry, not the current process env)
      var pathAfterInstall =
        Environment.GetEnvironmentVariable( "PATH", EnvironmentVariableTarget.User ) ?? string.Empty;
      var pathEntries = pathAfterInstall.Split( ';' );

      Assert.That(
        pathEntries,
        Contains.Item( installDir ),
        $"Expected '{installDir}' to be present in User PATH after install"
      );
    }
    finally {
      // Restore User PATH to what it was before the test
      Environment.SetEnvironmentVariable( "PATH", originalPath, EnvironmentVariableTarget.User );
      DeleteBestEffort( installDir );
    }
  }

  [TestCase( "pwsh" )]
  [TestCase( "powershell" )]
  public async Task InstallDoesNotDuplicateInUserPath( string shell ) {
    var tempDir = Path.GetTempPath();
    var installDir = Path.Combine( tempDir, "drift-install-ps-nodup-" + Guid.NewGuid() );
    Directory.CreateDirectory( installDir );

    // Capture original User PATH for restore
    var originalPath = Environment.GetEnvironmentVariable( "PATH", EnvironmentVariableTarget.User ) ?? string.Empty;

    // Pre-populate User PATH with installDir so the script's "already present" branch is taken
    var preSeedEntries = originalPath.Split( ';' )
      .Where( e => !e.Equals( installDir, StringComparison.OrdinalIgnoreCase ) )
      .Append( installDir );
    Environment.SetEnvironmentVariable( "PATH", string.Join( ";", preSeedEntries ), EnvironmentVariableTarget.User );

    try {
      // Act: run install twice to make sure no duplicates accumulate either
      for ( var i = 0; i < 2; i++ ) {
        var installProcess =
          await new ToolWrapper( shell, new() { { "DRIFT_INSTALL_DIR", installDir } } )
            .ExecuteAsync( $"-NonInteractive -File \"{InstallScript}\"" );

        PrintInstallOutput( installProcess, shell );
        Assert.That( installProcess.ExitCode, Is.EqualTo( ScriptExitCodeSuccess ) );
      }

      // Assert: exactly one occurrence of installDir in User PATH
      var pathAfterInstall =
        Environment.GetEnvironmentVariable( "PATH", EnvironmentVariableTarget.User ) ?? string.Empty;
      var occurrences = pathAfterInstall
        .Split( ';' )
        .Count( e => e.Equals( installDir, StringComparison.OrdinalIgnoreCase ) );

      Assert.That(
        occurrences,
        Is.EqualTo( 1 ),
        $"Expected exactly one entry for '{installDir}' in User PATH, but found {occurrences}"
      );
    }
    finally {
      Environment.SetEnvironmentVariable( "PATH", originalPath, EnvironmentVariableTarget.User );
      DeleteBestEffort( installDir );
    }
  }
}