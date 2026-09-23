using Drift.Cli.Abstractions;
using Drift.Cli.Settings.Serialization;
using Drift.Cli.Settings.V1_preview;
using Drift.Cli.Settings.V1_preview.Environments;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Infrastructure;

internal sealed class EnvironmentTargetProvider(
  ISettingsLocation settingsLocation,
  ILogger logger
) {
  public EnvironmentTarget Resolve() {
    var settings = CliSettings.Read( settingsLocation, logger );
    if ( string.IsNullOrWhiteSpace( settings.ActiveEnvironment ) ||
         string.Equals( settings.ActiveEnvironment, BuiltInEnvironmentNames.Local, StringComparison.OrdinalIgnoreCase )
       ) {
      return EnvironmentTarget.Local;
    }

    if ( !settings.TryGetEnvironment( settings.ActiveEnvironment, out var environment ) || environment == null ) {
      throw new InvalidOperationException( $"Active environment '{settings.ActiveEnvironment}' does not exist." );
    }

    return EnvironmentTarget.ForServer( NormalizeAddress( environment.Address ) );
  }

  internal static Uri NormalizeAddress( string address ) {
#pragma warning disable S5332
    var candidate = address.Contains( "://", StringComparison.Ordinal ) ? address : $"http://{address}";
#pragma warning restore S5332
    if ( !Uri.TryCreate( candidate, UriKind.Absolute, out var uri ) ) {
      throw new FormatException( $"Environment address '{address}' is not a valid URI." );
    }

    return uri;
  }
}