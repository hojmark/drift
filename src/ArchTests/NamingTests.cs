using ArchUnitNET.Domain;
using ArchUnitNET.NUnit;
using Drift.ArchTests.Fixtures;
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

  [Test]
  public void TestClassesShouldEndWithTests() {
    // TODO enable for TUnit when migrated
    var rule = Members().That()
      .HaveAnyAttributes( typeof(TestAttribute) /*, typeof(TUnit.Core.TestAttribute)*/ )
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