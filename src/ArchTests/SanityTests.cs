using Drift.ArchTests.Fixtures;

namespace Drift.ArchTests;

public class SanityTests : DriftArchitectureFixture {
  [Test]
  public void FindManyAssemblies() {
    foreach ( var assembly in DriftArchitecture.Assemblies ) {
      Console.WriteLine( assembly.Name );
    }

    Assert.That(
      DriftArchitecture.Assemblies.Count,
      Is.GreaterThanOrEqualTo( 20 ),
      "A significant amount of assemblies was not found"
    );
  }

  //TODO test that all test assemblies have an NUnit category assigned
}