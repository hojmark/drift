namespace Drift.TestUtilities;

// TODO DUPLICATE: move to shared project
public static class Environment {
  public static bool IsCi() {
    return System.Environment.GetEnvironmentVariable( "CI" ) == "true";
  }
}