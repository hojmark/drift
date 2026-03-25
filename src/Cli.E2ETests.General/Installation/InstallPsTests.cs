using Drift.Common;

namespace Drift.Cli.E2ETests.General.Installation;

[Platform( "Win" )]
internal sealed partial class InstallPsTests {
  private const int ExitCodeSuccess = 0;
  private const int ExitCodeFailure = 1;
  private static readonly string InstallScript = GetInstallScript();

  private static string GetInstallScript() {
    var repoRoot = TestContext.CurrentContext.TestDirectory;
    while ( !File.Exists( Path.Combine( repoRoot, "install.ps1" ) ) && repoRoot != Path.GetPathRoot( repoRoot ) ) {
      repoRoot = Path.GetDirectoryName( repoRoot )!;
    }

    var installScript = Path.Combine( repoRoot, "install.ps1" );
    Assert.That( File.Exists( installScript ), $"Could not find install.ps1 at repo root: {installScript}" );

    return installScript;
  }

  private static void DeleteBestEffort( params string[] paths ) {
    foreach ( var path in paths ) {
      try {
        if ( Directory.Exists( path ) ) {
          Directory.Delete( path, true );
        }
        else {
          File.Delete( path );
        }
      }
      catch {
        // best-effort cleanup
      }
    }
  }

  private static async Task AssertShellIsAvailable( string shell ) {
    var check = await new ToolWrapper( shell ).ExecuteAsync( "-NonInteractive -Command \"exit 0\"" );
    Assert.That(
      check.ExitCode,
      Is.EqualTo( 0 ),
      $"Pre-condition failed: '{shell}' is not available or not functional on this machine (exit code: {check.ExitCode})"
    );
  }

  private static void PrintInstallOutput(
    (string StdOut, string ErrOut, int ExitCode, bool Cancelled) result,
    string shell
  ) {
    Console.WriteLine( "--------------- install.ps1 output ({shell}) -----------------" );

    Console.WriteLine( result.StdOut );

    if ( !string.IsNullOrWhiteSpace( result.ErrOut ) ) {
      Console.WriteLine( $"\nSTDERR:\n {result.ErrOut}" );
    }

    Console.WriteLine( "--------------------------------------------------------------" );
  }
}