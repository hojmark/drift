namespace Drift.Build.Utilities;

public static class Environment {
  public static bool IsCi() {
    return System.Environment.GetEnvironmentVariable( "CI" ) == "true";
  }
}