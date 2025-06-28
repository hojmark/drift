using Drift.Spec.Schema;
using Drift.Spec.Schema.Generation;

namespace Drift.Cli.E2ETests.Schema;

public class SchemaGenerationTests {
  [TestCase( SpecVersion.V1_preview )]
  public async Task GenerateSpecTest( SpecVersion version ) {
    var schema = SchemaGenerator.Generate( version );
    await Verify( schema );
  }
}