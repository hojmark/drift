using Nuke.Common.Tools.DotNet;

public static class DotNetTestSettingsExtensions {
  internal static DotNetTestSettings ConfigureLoggers( this DotNetTestSettings settings, string verbosity ) {
    return settings
      .AddLoggers( $"\"console;verbosity={verbosity}\"" )
      .AddLoggers( "trx" );
  }
}