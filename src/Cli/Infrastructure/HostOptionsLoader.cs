using Drift.Hosting.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Infrastructure;

/// <summary>
/// Resolves host (server/agent) options from the standard configuration sources.
/// Precedence, lowest to highest: option defaults, JSON file, environment variables. Explicit command-line
/// options are applied by the caller on top of the result.
/// </summary>
internal static class HostOptionsLoader {
  /// <summary>
  /// Builds the configuration and binds it using <paramref name="bind"/>.
  /// </summary>
  /// <typeparam name="TOptions">Host options type.</typeparam>
  /// <param name="file">Configuration file in the config directory. May be absent.</param>
  /// <exception cref="HostConfigurationException">Configuration could not be read or bound.</exception>
  internal static TOptions Load<TOptions>(
    string file,
    string environmentVariablePrefix,
    Func<IConfiguration, TOptions> bind,
    ILogger logger
  ) {
    if ( File.Exists( file ) ) {
      logger.LogDebug( "Reading configuration file: {Path}", file );
    }
    else {
      logger.LogDebug( "Configuration file not found: {Path}", file );
    }

    IConfigurationRoot? configuration = null;
    try {
      configuration = new ConfigurationBuilder()
        .AddDriftHostSources( file, environmentVariablePrefix )
        .Build();

      return bind( configuration );
    }
    catch ( Exception e ) when ( e is InvalidDataException or FormatException or InvalidOperationException ) {
      throw new HostConfigurationException(
        $"Invalid configuration (file: {file}, environment variables: {environmentVariablePrefix}*): {e.Message}",
        e
      );
    }
    finally {
      ( configuration as IDisposable )?.Dispose();
    }
  }
}

/// <summary>
/// Thrown when host configuration (file or environment variables) cannot be read or bound.
/// </summary>
// Justified: no consumers as this is an executable assembly
#pragma warning disable S3871
internal sealed class HostConfigurationException( string message, Exception innerException )
#pragma warning restore S3871
  : Exception( message, innerException );