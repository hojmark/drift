using Drift.Spec.Schema;
using Drift.Spec.Schema.Generation;

namespace Drift.Spec.Tests;

[TestFixture]
public class SchemaTests {
  [TestCase( SpecVersion.V1_preview )]
  public async Task SchemaTest( SpecVersion version ) {
    // Arrange / Act
    var spec = SchemaGenerator.Generate( version );

    // Assert
    await VerifyJson( spec );
  }
}