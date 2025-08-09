namespace Drift.Cli.Abstractions;

/// <summary>
/// Provides standard exit codes for the Drift CLI.
/// </summary>
public static class ExitCodes {
  /// <summary>
  /// Indicates that the operation completed successfully.
  /// </summary>
  public const int Success = 0;

  /// <summary>
  /// Indicates that an unknown error occurred.
  /// </summary>
  public const int UnknownError = -1;

  /// <summary>
  /// The default error code returned from System.CommandLine actions.
  /// </summary>
  public const int SystemCommandLineDefaultError = 1;

  /// <summary>
  /// Indicates that a general (non-specific) error occurred.
  /// </summary>
  public const int GeneralError = 2;

  /// <summary>
  /// Indicates that a spec validation error occurred.
  /// </summary>
  public const int SpecValidationError = 3;
}