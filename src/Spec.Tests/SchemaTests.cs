using Drift.Spec.Schema;

namespace Drift.Spec.Tests;

internal sealed class SchemaTests {
  [TestCase( SpecVersion.V1_preview )]
  public void EmbeddedSchemaIsUpdated( SpecVersion version ) {
    // Arrange / Act
    var runtimeGeneratedSchema = SchemaGenerator.Generate( version );
    var embeddedSchema = SpecSchemaProvider.GetAsText( version );

    // Assert
    Assert.That( runtimeGeneratedSchema, Is.EqualTo( embeddedSchema ), "Embedded schema is not up to date" );
  }
}