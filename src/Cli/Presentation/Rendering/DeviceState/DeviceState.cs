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