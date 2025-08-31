using Drift.ArchTests.Fixtures;

namespace Drift.ArchTests;

public class SanityTests : DriftArchitectureFixture {
  private const uint ExpectedAssemblyCount = 17;
  private const uint ExpectedAssemblyCountTolerance = 3;

  [Test]
  public void FindManyAssemblies() {
    foreach ( var assembly in DriftArchitecture.Assemblies ) {
      Console.WriteLine( assembly.Name );
    }

    Assert.That(
      DriftArchitecture.Assemblies.Count,
      Is.InRange(
        ExpectedAssemblyCount - ExpectedAssemblyCountTolerance,
        ExpectedAssemblyCount + ExpectedAssemblyCountTolerance
      ),
      "A significant amount of assemblies was not found"
    );
  }

  //TODO test that all test assemblies have an NUnit category assigned
}