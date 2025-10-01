namespace Drift.Cli.Presentation.Rendering.DeviceState;

internal enum DeviceState {
  Undefined,
  KnownExpectedOnline,
  KnownExpectedOffline,
  KnownUnexpectedOnline,
  KnownUnexpectedOffline,
  KnownDynamicOnline,
  KnownDynamicOffline,
  UnknownAllowed,
  UnknownDisallowed,
}

internal static class DeviceStateExtensions {
  public static bool IsConformant( this DeviceState state ) {
    switch ( state ) {
      case DeviceState.KnownExpectedOnline:
      case DeviceState.KnownExpectedOffline:
      case DeviceState.KnownDynamicOnline:
      case DeviceState.KnownDynamicOffline:
        return true;
      case DeviceState.KnownUnexpectedOnline:
      case DeviceState.KnownUnexpectedOffline:
      case DeviceState.UnknownAllowed:
      case DeviceState.UnknownDisallowed:
      case DeviceState.Undefined:
        return false;
      default:
        throw new ArgumentOutOfRangeException( nameof(state), state, null );
    }
  }
}