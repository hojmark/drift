using Drift.TestUtilities.ResourceProviders;
using Environment = Drift.Domain.Environment;

namespace Drift.EnvironmentConfig.Tests;

//TODO move out of this project
internal sealed class EnvConfigTest {
  //[Explicit( "Not a feature yet" )]
  //[Test]
  public void CanDeserializeEnvConfigTest() {
    var stream = LocalTestResourceProvider.GetStream( "drift-env.json" );
    var environment = JsonConverter.Deserialize<Environment>( stream );
    Verify( environment );
  }
}