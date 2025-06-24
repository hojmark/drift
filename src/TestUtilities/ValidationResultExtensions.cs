namespace Drift.TestUtilities;

public static class ValidationResultExtensions {
  public static string ToUnitTestMessage( this object result ) {
    return "Expected YAML to be valid, but it was not:\n" + result;
  }
}