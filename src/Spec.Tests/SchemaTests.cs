using Drift.Spec.Schema;
using Drift.Spec.Schema.Generation;

namespace Drift.Spec.Tests;

[TestFixture]
public class SchemaTests {
  [TestCase( SpecVersion.V1_preview )]
  public void EmbeddedSchemaIsUpdated( SpecVersion version ) {
    // Arrange / Act
    var runtimeGeneratedSchema = SchemaGenerator.Generate( version );
    var embeddedSchema = SpecSchemaProvider.AsText( version );

    // Assert
    Assert.That( runtimeGeneratedSchema, Is.EqualTo( embeddedSchema ), "Embedded schema is not up to date" );
  }
}