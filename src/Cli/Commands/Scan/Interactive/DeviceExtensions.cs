using Drift.Domain.Device.Declared;

namespace Drift.Cli.Commands.Scan.Interactive;

//TODO location?
internal static class DeclaredDeviceExtensions {
  private const bool DefaultEnabled = true;

  //TODO name?
  internal static bool IsEnabled( this DeclaredDevice device ) {
    return device.Enabled ?? DefaultEnabled;
  }
}