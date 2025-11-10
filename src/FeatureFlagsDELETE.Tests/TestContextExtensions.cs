namespace Drift.FeatureFlagsDELETE.Tests;

public static class TestContextExtensions {
  public static HashSet<FeatureFlag> GetFeatureFlags( this TestContext testContext ) {
    var getFlags = (Func<HashSet<FeatureFlag>>) TestContext.CurrentContext.Test.Properties.Get( "GetFeatureFlags" )!;
    return getFlags();
  }
}