using ArchUnitNET.Domain;
using ArchUnitNET.NUnit;
using Drift.ArchTests.Fixtures;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Drift.ArchTests;

public class DependencyTests : DriftArchitectureFixture {
  private readonly IObjectProvider<IType> _cliAssemlblyTypes =
    Types().That()
      .ResideInAssemblyMatching( @"Drift\.Cli$" )
      .Or()
      .ResideInAssemblyMatching( @"Drift\.Cli\..*" )
      .As( "Cli* assemblies" );

  private readonly IObjectProvider<IType> _testAssemblyTypes =
    Types().That()
      .ResideInAssemblyMatching( @".*\.Tests$" )
      .Or()
      .ResideInAssemblyMatching( @".*\.E2ETests$" )
      .As( "Tests" );

  [Test]
  public void NoAssemblyShouldDependOnTestAssemblies() {
    var rule = Types().Should()
      .NotDependOnAny( _testAssemblyTypes )
      .Because( "no assembly should depend on test assemblies" );

    rule.Check( DriftArchitecture );
  }

  [Test]
  public void NoNonCliAssembliesShouldDependOnCliAssemblies() {
    var nonCliAssemblies = Types().That().AreNot( _cliAssemlblyTypes );

    var rule = nonCliAssemblies
      .Should()
      .NotDependOnAny( _cliAssemlblyTypes )
      .Because( "they are presentation layer" );

    rule.Check( DriftArchitecture );
  }
}