using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.NUnit;
using Drift.ArchTests.Fixtures;
using Drift.Common.IO;
using Drift.Domain.Device;
using Drift.Networking.Core.Abstractions;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Drift.ArchTests;

internal sealed class NamingTests : DriftArchitectureFixture {
  [Test]
  public void InterfacesShouldStartWithI() {
    var rule = Interfaces()
      .Should()
      .HaveNameStartingWith( "I" )
      .Because( "Interface naming convention should be followed" );

    rule.Check( DriftArchitecture );
  }

  [TestCase( new[] { typeof(RequestHandler<,>), typeof(StreamingRequestHandler<,,>) }, 2 )]
  [TestCase( new[] { typeof(IAddressableDevice) }, 1 )]
  [TestCase( new[] { typeof(IDriftSettingsLocation) }, 3 )]
  [TestCase( new[] { typeof(IDriftDataLocation) }, 3 )]
  public void DescendantsShouldEndWithImplementedTypeSuffix( Type[] handlerTypes, int wordCount ) {
    foreach ( var handlerType in handlerTypes ) {
      // Remove generic arity ("RequestHandler`2" -> "RequestHandler")
      string cleanBaseName = Regex.Replace( handlerType.Name, @"`\d+", string.Empty );

      // Split name based on PascalCase ("StreamingRequestHandler" -> ["Streaming", "Request", "Handler"])
      var words = Regex.Matches( cleanBaseName, "[A-Z][a-z0-9]*" )
        .Select( m => m.Value )
        .ToArray();

      if ( words.Length == 0 ) {
        Assert.Fail( $"Could not determine words from type name: '{handlerType.Name}'" );
      }

      // Take n last words
      int takeCount = Math.Min( wordCount, words.Length );
      string expectedSuffix = string.Join( string.Empty, words.TakeLast( takeCount ) );

      var implementations = Classes().That()
        .AreAssignableTo( handlerType )
        .GetObjects( DriftArchitecture )
        .Where( type => type.FullName != handlerType.FullName )
        .DistinctBy( type => type.FullName )
        .ToArray();

      var incorrectlyNamed = implementations
        .Where( type => !type.Name.EndsWith( expectedSuffix, StringComparison.Ordinal ) )
        .Select( type => type.FullName )
        .ToArray();

      Assert.That(
        incorrectlyNamed,
        Is.Empty,
        $"{handlerType.Name} implementations should end with '{expectedSuffix}', but some did not."
      );
    }
  }

  [Test]
#pragma warning disable S2699
  public void ListAllInterfacesInSolution() {
#pragma warning restore S2699
    var interfaces = Interfaces()
      .GetObjects( DriftArchitecture )
      .Select( type => type.FullName )
      .OrderBy( name => name )
      .ToArray();

    Console.WriteLine( $"Found {interfaces.Length} interfaces:\n\n" + string.Join( "\n", interfaces ) );
  }

  [Test]
  public void TestClassesShouldEndWithTests() {
    var rule = Members().That()
      .HaveAnyAttributes( typeof(TestAttribute) )
      .Should()
      .BeDeclaredInTypesThat()
      .HaveNameEndingWith( "Tests" )
      .Because( "test classes should follow naming convention" );

    rule.Check( DriftArchitecture );
  }

  // Justification: for debugging
#pragma warning disable S1144
  private static void PrintTypes( IObjectProvider<IType> types ) {
#pragma warning restore S1144
    var classes = types.GetObjects( DriftArchitecture ).ToList();

    if ( classes.Count == 0 ) {
      Console.WriteLine( "[none]" );
    }

    foreach ( var testClass in classes ) {
      Console.WriteLine( testClass.FullName );
    }
  }
}