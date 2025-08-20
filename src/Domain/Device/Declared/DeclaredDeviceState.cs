namespace Drift.Domain.Device.Declared;

public enum DeclaredDeviceState {
  /// <summary>
  /// Device must always be up (online)
  /// </summary>
  Up = 1,

  /// <summary>
  /// Device can be up or down (no strict requirement)
  /// </summary>
  Dynamic = 2,

  /// <summary>
  /// Device should always be down (offline)
  /// </summary>
  Down = 3
}