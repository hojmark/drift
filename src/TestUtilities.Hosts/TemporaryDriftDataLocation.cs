using Drift.Common.IO;

namespace Drift.TestUtilities.Hosts;

public sealed class TemporaryDriftDataLocation : IDriftDataLocation {
  public string Directory {
    get;
  } = System.IO.Directory.CreateTempSubdirectory( "drift-data-test-" ).FullName;
}