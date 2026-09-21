using Drift.Common.IO;

namespace Drift.Cli.Tests.Utils.Coordinator;

internal sealed class TemporaryDriftDataLocation : IDriftDataLocation {
  private readonly string _directory = System.IO.Directory.CreateTempSubdirectory( "drift-data-test-" ).FullName;

  public string Directory => _directory;
}