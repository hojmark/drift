/*using Drift.Parsers.SpecYaml.Generators;
using Drift.Parsers.SpecYaml.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Drift.Parsers.SpecYaml.Tests;

public class SourceGenerationTests {
  [Test]
  public async Task Asd2() {
    const string source = @"
using Drift.Parsers.SpecYaml.Generators;

[SpecRoot2]
public record MyRootRecord {
  public string Id {
    get;
    set;
  }
}
";

    await Verify( source );
  }

  private static Task Verify( string source ) {
    // Parse the provided string into a C# syntax tree
    var syntaxTree = CSharpSyntaxTree.ParseText( source );

    var references = Basic.Reference.Assemblies.Net90.References.All.ToList();
    references.Add( MetadataReference.CreateFromFile( typeof(SchemaGenerator).Assembly.Location ) );
 //   references.Add( MetadataReference.CreateFromFile( typeof(SpecRootAttribute).Assembly.Location ) );

    // Create a Roslyn compilation for the syntax tree.
    var compilation = CSharpCompilation.Create(
      assemblyName: "TemporaryUnitTestAssembly",
      syntaxTrees: [syntaxTree],
      references: references,
      options: new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary )
    );

    // Check for compilation errors first
    var diagnostics = compilation.GetDiagnostics()
      .Where( d => d.Severity == DiagnosticSeverity.Error )
      .ToList();

    if ( diagnostics.Any() ) {
      Console.WriteLine( "Compilation errors:" );
      foreach ( var diagnostic in diagnostics ) {
        Console.WriteLine( $"  {diagnostic}" );
      }
    }

    var specRootGenerator = new SpecRootAttributeGenerator();
    var generator = new SchemaGenerator();

    // Run in multiple passes to ensure attribute is available
    GeneratorDriver driver = CSharpGeneratorDriver.Create(specRootGenerator);

    // First pass: Generate the attribute
    driver = driver.RunGenerators(compilation);
    var firstResult = driver.GetRunResult();

    // Update compilation with generated attribute
    var updatedCompilation = compilation.AddSyntaxTrees(firstResult.GeneratedTrees);

    // Second pass: Run the main generator with attribute available
    driver = CSharpGeneratorDriver.Create(generator);
    driver = driver.RunGenerators(updatedCompilation);

    var res = driver.GetRunResult();


    // Debug output
    Console.WriteLine( $"Generated {res.GeneratedTrees.Length} source files" );
    Console.WriteLine( $"Generator diagnostics: {res.Diagnostics.Length}" );

    foreach ( var diagnostic in res.Diagnostics ) {
      Console.WriteLine( $"Generator diagnostic: {diagnostic}" );
    }

    foreach ( var tree in res.GeneratedTrees ) {
      Console.WriteLine( $"Generated file: {tree.FilePath}" );
      Console.WriteLine( "Content:" );
      Console.WriteLine( tree.GetText().ToString() );
      Console.WriteLine( "---" );
    }

    // Use verify to snapshot test the source generator output!
    return Verifier.Verify( driver ).UseDirectory( "Snapshots" );
  }
}*/