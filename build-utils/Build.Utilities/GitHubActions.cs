namespace Drift.Build.Utilities;

public static class GitHubActions {
  public static void SetOutput( string name, string value ) {
    var githubOutputFile = System.Environment.GetEnvironmentVariable( "GITHUB_OUTPUT" );
    if ( githubOutputFile != null ) {
      File.AppendAllText( githubOutputFile, $"{name}={value}\n" );
    }
  }
}