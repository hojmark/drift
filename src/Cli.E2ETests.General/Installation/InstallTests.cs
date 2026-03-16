namespace Drift.Cli.E2ETests.General.Installation;

[Platform( "Linux" )]
internal sealed partial class InstallTests {
  private static readonly string InstallScript = GetInstallScript();

  private static string GetInstallScript() {
    var repoRoot = TestContext.CurrentContext.TestDirectory;
    while ( !File.Exists( Path.Combine( repoRoot, "install.sh" ) ) && repoRoot != "/" ) {
      repoRoot = Path.GetDirectoryName( repoRoot )!;
    }

    var installScript = Path.Combine( repoRoot, "install.sh" );
    Assert.That( File.Exists( installScript ), $"Could not find install.sh at repo root: {installScript}" );

    return installScript;
  }

  private static void PrintInstallOutput( (string StdOut, string ErrOut, int ExitCode, bool Cancelled) result ) {
    Console.WriteLine( "------------------- install.sh output ----------------------" );
    Console.WriteLine( result.StdOut );
    if ( !string.IsNullOrWhiteSpace( result.ErrOut ) ) {
      Console.WriteLine( $"\nSTDERR:\n {result.ErrOut}" );
    }

    Console.WriteLine( "------------------------------------------------------------" );
  }
}