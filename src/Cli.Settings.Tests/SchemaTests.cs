using Drift.Cli.Settings.Schema;

namespace Drift.Cli.Settings.Tests;

internal sealed class SchemaTests {
  [TestCase( SettingsVersion.V1_preview )]
  public void EmbeddedSchemaIsUpdated( SettingsVersion version ) {
    // Arrange / Act
    var runtimeGeneratedSchema = SchemaGenerator.Generate( version );
    var embeddedSchema = SettingsSchemaProvider.AsText( version );

    // Assert
    Assert.That( runtimeGeneratedSchema, Is.EqualTo( embeddedSchema ), "Embedded schema is not up to date" );
  }
}