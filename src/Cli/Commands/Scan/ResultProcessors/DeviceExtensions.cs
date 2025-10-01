using Drift.Domain.Device.Declared;

namespace Drift.Cli.Commands.Scan.ResultProcessors;

//TODO location?
internal static class DeclaredDeviceExtensions {
  private const bool EnabledByDefault = true;

  //TODO name?
  internal static bool IsEnabled( this DeclaredDevice device ) {
    return device.Enabled ?? EnabledByDefault;
  }
}