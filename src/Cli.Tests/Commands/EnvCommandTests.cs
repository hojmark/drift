using Drift.Cli.Settings.Serialization;
using Drift.Cli.Settings.Tests;
using Drift.Cli.Settings.V1_preview;
using Drift.Cli.Settings.V1_preview.Environments;
using Drift.Cli.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class EnvCommandTests {
  private ISettingsLocationProvider SettingsLocation {
    get;
  } = new TemporarySettingsLocationProvider();

  private Task<CliCommandResult> InvokeAsync( string args ) {
    return DriftTestCli.InvokeAsync( args, settingsLocation: SettingsLocation );
  }

  private CliSettings ReadSettings() {
    return CliSettings.Read( location: SettingsLocation );
  }

  private void WriteSettings( CliSettings settings ) {
    settings.Write( NullLogger.Instance, location: SettingsLocation );
  }

  [TearDown]
  public void TearDown() {
    var settingsDir = SettingsLocation.GetDirectory();
    if ( Directory.Exists( settingsDir ) ) {
      Directory.Delete( settingsDir, true );
    }
  }

  private void CreateInitialEnvironment( string name, string address ) {
    CreateInitialEnvironments( ( name, address ) );
  }

  private void CreateInitialEnvironments( params (string name, string address)[] environments ) {
    var settings = new CliSettings {
      Environments = environments
        .Select( e => new EnvironmentSetting( e.name, e.address ) )
        .ToList(),
      ActiveEnvironment = environments.Length > 0 ? environments[0].name : null
    };

    WriteSettings( settings );
  }
}