namespace Drift.Cli.Tests.Utils;

/// <summary>
/// Points <c>XDG_DATA_HOME</c> at a temporary directory for the lifetime of the instance, so that the default
/// data location (<see cref="Drift.Common.IO.DriftDirectories.DataDirectory"/>) can be exercised on Linux without
/// touching the user's real data directory.
/// </summary>
/// <remarks>
/// Mutates process-wide state: tests using this must be <see cref="NonParallelizableAttribute"/>.
/// </remarks>
internal sealed class TemporaryXdgDataHome : IDisposable {
  private const string VariableName = "XDG_DATA_HOME";

  private readonly string? _previousValue = Environment.GetEnvironmentVariable( VariableName );

  public TemporaryXdgDataHome() {
    Environment.SetEnvironmentVariable( VariableName, Directory );
  }

  public string Directory {
    get;
  } = System.IO.Directory.CreateTempSubdirectory( "drift-xdg-data-test-" ).FullName;

  /// <summary>
  /// Gets the Drift data directory expected to be resolved under the temporary <c>XDG_DATA_HOME</c>.
  /// </summary>
  public string DriftDataDirectory => Path.Combine( Directory, "drift" );

  public void Dispose() {
    Environment.SetEnvironmentVariable( VariableName, _previousValue );
    System.IO.Directory.Delete( Directory, recursive: true );
  }
}