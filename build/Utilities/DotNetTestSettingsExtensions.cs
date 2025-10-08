using Nuke.Common.Tools.DotNet;

namespace Utilities;

internal static class DotNetTestSettingsExtensions {
  internal static DotNetTestSettings ConfigureLoggers( this DotNetTestSettings settings, string verbosity ) {
    return settings
      .AddLoggers( $"\"console;verbosity={verbosity}\"" )
      .AddLoggers( "trx" );
  }
}