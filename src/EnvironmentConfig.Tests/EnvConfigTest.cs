using Drift.TestUtilities.ResourceProviders;
using Environment = Drift.Domain.Environment;

namespace Drift.EnvironmentConfig.Tests;

// TODO move out of this project
internal sealed class EnvConfigTest {
  // [Explicit( "Not a feature yet" )]
  // [Test]
#pragma warning disable S2325
  public void CanDeserializeEnvConfigTest() {
#pragma warning restore S2325
    var stream = LocalTestResourceProvider.GetStream( "drift-env.json" );
    var environment = JsonConverter.Deserialize<Environment>( stream );
    Verify( environment );
  }
}