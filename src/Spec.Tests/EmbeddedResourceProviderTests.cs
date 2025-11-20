using Drift.Common.EmbeddedResources;
using Drift.Spec.Schema;

namespace Drift.Spec.Tests;

internal sealed class EmbeddedResourceProviderTests {
  [Test]
  public void ResourceFound() {
    var path = $"schemas/{SpecVersion.V1_preview.ToJsonSchemaFileName()}";
    var stream = EmbeddedResourceProvider.GetStream( path );
    var contents = stream.ReadText();
    Assert.That( contents.Length, Is.GreaterThan( 100 ) );
  }

  [Test]
  public void ResourceNotFound() {
    var exception = Assert.Throws<Exception>( () => EmbeddedResourceProvider.GetStream( "notthere.json" ) );

    Assert.That( exception.Message, Is.EqualTo(
        """
        Resource does not exist: notthere.json
        Resolved assembly path: Drift.Spec.embedded_resources.notthere.json
        Available resources:
        - Drift.Spec.embedded_resources.schemas.drift-spec-v1-preview.schema.json
        """
      )
    );
  }
}