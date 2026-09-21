using Drift.ArchTests.Fixtures;

namespace Drift.ArchTests;

internal sealed class SanityTests : DriftArchitectureFixture {
  private const uint ExpectedAssemblyCount = 30;
  private const uint ExpectedAssemblyCountTolerance = 10;

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

  // TODO test that all test assemblies have an NUnit category assigned
}