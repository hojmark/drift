using Microsoft.Extensions.Configuration;

namespace Drift.Hosting.Configuration;

public static class ConfigurationBuilderExtensions {
  extension( IConfigurationBuilder builder ) {
    /// <summary>
    /// Adds the standard Drift host configuration sources. Later sources take precedence:
    /// <list type="number">
    ///   <item>JSON file at <paramref name="filePath"/> (optional)</item>
    ///   <item>Environment variables starting with <paramref name="environmentVariablePrefix"/> (prefix is stripped; <c>__</c> separates sections)</item>
    /// </list>
    /// </summary>
    /// <param name="filePath">Path to the JSON configuration file. A missing file is not an error.</param>
    /// <param name="environmentVariablePrefix">E.g. <c>DRIFT_SERVER_</c>.</param>
    public IConfigurationBuilder AddDriftHostSources( string filePath, string environmentVariablePrefix ) {
      return builder
        .AddJsonFile( Path.GetFullPath( filePath ), optional: true, reloadOnChange: false )
        .AddEnvironmentVariables( environmentVariablePrefix );
    }
  }
}