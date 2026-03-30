using System.Text.RegularExpressions;
using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils;
using Drift.TestUtilities;
using Environment = Drift.TestUtilities.Environment;

namespace Drift.Cli.Tests;

internal sealed class BadSettingsFileTests {
  private string? _tempDir;
  private string? _previousConfigDir;

  [OneTimeSetUp]
  public async Task SetUp() {
    _tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
    Directory.CreateDirectory( _tempDir );
    await File.WriteAllTextAsync( Path.Combine( _tempDir, Files.SettingsFileName ), "garbage" );

    _previousConfigDir = System.Environment.GetEnvironmentVariable( nameof(EnvVar.Drift_ConfigDir) );
    System.Environment.SetEnvironmentVariable( nameof(EnvVar.Drift_ConfigDir), _tempDir );
  }

  [OneTimeTearDown]
  public void TearDown() {
    System.Environment.SetEnvironmentVariable( nameof(EnvVar.Drift_ConfigDir), _previousConfigDir );
    Directory.Delete( _tempDir!, true );
  }

  [Test]
  public async Task BadSettingsFile_FallsBackToDefault(
    [Values( Platform.Linux, Platform.Windows )]
    Platform platform
  ) {
    Environment.SkipIfNot( platform );

    // Act
    var (exitCode, output, error) = await DriftTestCli.InvokeFromTestAsync(
      "lint ../../../../Spec.Tests/resources/network_single_subnet.yaml"
    );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) ); // Success because CLI should be resilient
      var combined = Regex.Replace(
        output.ToString() + error,
        "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
        "<guid>"
      );
      await Verify( combined )
        .UseParameters( platform );
    }
  }
}