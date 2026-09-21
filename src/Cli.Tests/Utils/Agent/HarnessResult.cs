using Drift.Domain;

namespace Drift.Cli.Tests.Utils.Agent;

/// <summary>
/// Result from running a scan with the agent test harness.
///
/// RECOMMENDED USAGE:
/// 1. Assert on exit codes: Assert.That(result.ScanExitCode, Is.EqualTo(ExitCodes.Success))
/// 2. Use Verify for output: await Verify(result.CombinedOutput)
/// 3. Query results if needed: result.Results.GetDevicesWithIp(...)
///
/// Only use result.Logs when you need to programmatically query specific log entries.
/// For most tests, Verify on CombinedOutput is easier to work with.
/// </summary>
internal sealed class HarnessResult {
  /// <summary>
  /// Gets the exit code from the scan command.
  /// </summary>
  public required int ScanExitCode {
    get;
    init;
  }

  /// <summary>
  /// Gets the standard output from the scan command.
  /// </summary>
  public required string ScanOutput {
    get;
    init;
  }

  /// <summary>
  /// Gets the standard error from the scan command.
  /// </summary>
  public required string ScanError {
    get;
    init;
  }

  /// <summary>
  /// Gets the combined output (stdout + stderr).
  /// </summary>
  public string CombinedOutput => ScanOutput + ScanError;

  /// <summary>
  /// Gets the agent IDs enrolled in the coordinator for this scan.
  /// </summary>
  public required IReadOnlyCollection<AgentId> EnrolledAgentIds {
    get;
    init;
  }
}