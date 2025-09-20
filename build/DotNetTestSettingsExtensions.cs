using Nuke.Common.Tools.DotNet;

internal static class DotNetTestSettingsExtensions {
  internal static DotNetTestSettings ConfigureLoggers( this DotNetTestSettings settings, string verbosity ) {
    return settings
      .AddLoggers( $"\"console;verbosity={verbosity}\"" )
      .AddLoggers( "trx" );
  }
}