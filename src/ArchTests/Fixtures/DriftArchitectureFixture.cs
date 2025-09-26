using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using Assembly = System.Reflection.Assembly;

namespace Drift.ArchTests.Fixtures;

internal abstract class DriftArchitectureFixture {
  protected static readonly Architecture DriftArchitecture = new ArchLoader()
    .LoadFilteredDirectory( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ), "Drift.*.dll" )
    .Build();
}