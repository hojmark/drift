using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Drift.Build.Utilities;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

sealed partial class NukeBuild {
  [Parameter( "Skip Containerlab deployment (useful for debugging when topology is already running)" )]
  readonly bool SkipClabDeploy = false;

  [Parameter( "Keep Containerlab topology running after tests" )]
  readonly bool KeepClabRunning = false;

  [Parameter( "Run only this topology (e.g. 'simple-test'). Runs all topologies if not specified." )]
  readonly string ClabTopology = null;

  /// <summary>
  /// Defines all Containerlab integration test cases.
  /// Each test case specifies a topology, its spec file, the CLI container name,
  /// and assertions to validate the scan output.
  /// </summary>
  private static readonly ContainerlabTestCase[] TestCases = [
    new(
      Name: "simple-test",
      TopologyFile: "simple-test.clab.yaml",
      SpecFile: "simple-test-spec.yaml",
      CliContainer: "clab-drift-simple-test-cli",
      CoordinatorAddress: "http://clab-drift-simple-test-server:51510",
      AgentIds: ["agent_test1"],
      SnapshotFile: "simple-test-scan-output.json"
    ),
    new(
      Name: "cooperation-test",
      TopologyFile: "cooperation-test.clab.yaml",
      SpecFile: "cooperation-test-spec.yaml",
      CliContainer: "clab-drift-cooperation-test-cli",
      CoordinatorAddress: "http://clab-drift-cooperation-test-server:51510",
      AgentIds: ["agent_coop_agent1", "agent_coop_agent2", "agent_coop_agent3"],
      SnapshotFile: "cooperation-test-scan-output.json"
    ),
    new(
      Name: "subnet-isolation-test",
      TopologyFile: "subnet-isolation-test.clab.yaml",
      SpecFile: "subnet-isolation-test-spec.yaml",
      CliContainer: "clab-drift-subnet-isolation-test-cli",
      CoordinatorAddress: "http://clab-drift-subnet-isolation-test-server:51510",
      AgentIds: ["agent_subnet_agent1", "agent_subnet_agent2"],
      SnapshotFile: "subnet-isolation-test-scan-output.json"
    ),
  ];

  Target TestE2E_Clab => _ => _
    .DependsOn( BuildContainerImage )
    .After( TestUnit, TestE2E_Container )
    .OnlyWhenDynamic( () => Platform != DotNetRuntimeIdentifier.win_x64 )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(TestE2E_Clab) );

        var imageRef = _driftImageRef ?? throw new ArgumentNullException( nameof(_driftImageRef) );
        Log.Information( "Using image {ImageRef} for Containerlab tests", imageRef );

        if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
          Log.Warning( "Containerlab tests require Linux. Skipping." );
          return;
        }

        if ( !await IsContainerlabAvailableAsync() ) {
          throw new Exception( "Containerlab does not appear to be installed or in PATH." );
        }

        var casesToRun = SelectTestCases();

        Log.Information(
          "Running {Count} Containerlab test case(s): {Names}",
          casesToRun.Length,
          string.Join( ", ", casesToRun.Select( tc => tc.Name ) )
        );

        var total = casesToRun.Length;
        var passed = 0;
        var failed = 0;

        foreach ( var testCase in casesToRun ) {
          var run = passed + failed + 1;
          Log.Information( "---------------------------------------------" );
          Log.Information( "{Name} ({Run}/{Total})", testCase.Name, run, total );
          Log.Information( "---------------------------------------------" );

          if ( await RunTestCaseAsync( testCase ) ) {
            passed++;
            Log.Information( "🟢 PASS: {Name}", testCase.Name );
          }
          else {
            failed++;
            Log.Error( "🔴 FAIL: {Name}", testCase.Name );
          }
        }

        Log.Information( "Containerlab integration tests: {Passed} passed, {Failed} failed", passed, failed );

        if ( failed > 0 ) {
          throw new Exception( $"{failed} Containerlab test case(s) failed" );
        }
      }
    );

  private ContainerlabTestCase[] SelectTestCases() {
    if ( ClabTopology == null ) {
      return TestCases;
    }

    Log.Warning( "Only selecting test case(s) matching topology '{Topology}'", ClabTopology );

    var selected = TestCases.Where( tc => tc.Name == ClabTopology ).ToArray();
    if ( !selected.Any() ) {
      throw new Exception(
        $"No test case found matching topology '{ClabTopology}'. " +
        $"Valid names: {string.Join( ", ", TestCases.Select( tc => tc.Name ) )}"
      );
    }

    return selected;
  }

  private async Task<bool> RunTestCaseAsync( ContainerlabTestCase testCase ) {
    var topoFile = Paths.ContainerlabsDirectory / testCase.TopologyFile;
    var specFile = Paths.ContainerlabsDirectory / testCase.SpecFile;

    if ( !File.Exists( topoFile ) ) {
      Log.Error( "Topology file not found: {File}", topoFile );
      return false;
    }

    if ( !File.Exists( specFile ) ) {
      Log.Error( "Spec file not found: {File}", specFile );
      return false;
    }

    try {
      if ( SkipClabDeploy ) {
        Log.Information( "Skipping deployment (--skip-clab-deploy)" );
      }
      else {
        await DeployTopologyAsync( testCase.TopologyFile );
      }

      await RunScanAndAssertAsync( specFile, testCase );
      return true;
    }
    catch ( Exception ex ) {
      Log.Error( "Test case '{Name}' failed: {Error}", testCase.Name, ex.Message );
      return false;
    }
    finally {
      if ( KeepClabRunning ) {
        Log.Information( "Keeping topology running (--keep-clab-running)" );
      }
      else {
        await DestroyTopologyAsync( testCase.TopologyFile );
      }
    }
  }

  private static async Task<bool> IsContainerlabAvailableAsync() {
    try {
      var versionOutput = await CommandRunner.RunAsync( "containerlab", "version" );
      Log.Debug( "\n{Version}", versionOutput );
      return true;
    }
    catch {
      return false;
    }
  }

  private static async Task DeployTopologyAsync( string topologyFile ) {
    Log.Information( "Deploying topology: {File}", topologyFile );

    DestroyTopologyIfExists( topologyFile );
    EnsureClabManagementNetwork();

    Clab(
      $"deploy --topo {topologyFile}",
      Paths.ContainerlabsDirectory,
      timeout: TimeSpan.FromMinutes( 5 )
    ).AssertZeroExitCode();

    // TODO try to disable fixed waiting
    // Log.Information( "Waiting for containers to be ready..." );
    // await Task.Delay( TimeSpan.FromSeconds( 10 ) );
  }

  private static void DestroyTopologyIfExists( string topologyFile ) {
    try {
      Clab(
        $"destroy --topo {topologyFile} --cleanup",
        Paths.ContainerlabsDirectory,
        timeout: TimeSpan.FromMinutes( 2 ),
        logOutput: false
      ).AssertZeroExitCode();
    }
    catch {
      Log.Debug( "No existing topology to destroy (or destroy failed — continuing)" );
    }
  }

  /// <summary>
  /// Pre-creates the 'clab' management network before deploying.
  ///
  /// Rootless Podman with pasta networking does NOT create kernel bridge interfaces.
  /// Containerlab always tries `ip link show br-&lt;network-id&gt;` immediately after
  /// creating a new network, which fatally fails ("Link not found") because no
  /// kernel bridge was created. However, when the network already exists,
  /// Containerlab skips the creation step and reuses it — avoiding the fatal lookup.
  ///
  /// Strategy: try to remove any stale 'clab' network (ignore failure — may be in
  /// use by another running topology), then create it. Ignore "already exists" errors
  /// from create — the important thing is the network is present before deploy.
  /// </summary>
  private static void EnsureClabManagementNetwork() {
    Log.Debug( "Pre-creating Containerlab management network..." );

    // Ignore failure — network may not exist yet, or may still be in use by another topology
    var rm = ProcessTasks.StartProcess( "docker", "network rm clab", logOutput: false );
    rm.WaitForExit();

    // Ignore failure — "network already exists" is acceptable; we just need it to be present
    var create = ProcessTasks.StartProcess(
      "docker", "network create --subnet 172.20.20.0/24 --ipv6 --subnet 3fff:172:20:20::/64 clab",
      logOutput: false
    );
    create.WaitForExit();

    Log.Debug( "Management network 'clab' ready" );
  }

  private static async Task DestroyTopologyAsync( string topologyFile ) {
    Log.Information( "Destroying topology: {File}", topologyFile );
    try {
      Clab(
        $"destroy --topo {topologyFile} --cleanup",
        Paths.ContainerlabsDirectory,
        timeout: TimeSpan.FromMinutes( 2 )
      ).AssertZeroExitCode();
    }
    catch ( Exception ex ) {
      Log.Warning( "Failed to destroy topology: {Error}", ex.Message );
    }
  }

  private static async Task RunScanAndAssertAsync( AbsolutePath specFile, ContainerlabTestCase testCase ) {
    Log.Information( "Running scan for test case: {Name}", testCase.Name );

    //Log.Debug( "Copying spec to CLI container {Container}...", testCase.CliContainer );
    Docker( $"cp {specFile} {testCase.CliContainer}:/tmp/spec.yaml" ).AssertZeroExitCode();

    //Log.Information( "Configuring the CLI to use coordinator {Address}...", testCase.CoordinatorAddress );
    RunCliCommand( testCase, $"env add container-server {testCase.CoordinatorAddress}" );
    await RunCliCommandWithRetryAsync( testCase, "spec apply /tmp/spec.yaml" );

    foreach ( var agentId in testCase.AgentIds ) {
      await RunCliCommandWithRetryAsync( testCase, $"enrollment add {agentId}" );
    }

    var scanResult = RunCliCommand( testCase, "scan --output Json", timeout: TimeSpan.FromMinutes( 5 ) );

    AssertScanSnapshot( testCase, scanResult.Output.Select( o => o.Text ) );
  }

  private static IProcess RunCliCommand( ContainerlabTestCase testCase, string command, TimeSpan? timeout = null ) {
    return Docker( $"exec {testCase.CliContainer} /app/drift {command}", timeout: timeout ).AssertZeroExitCode();
  }

  private static async Task<IProcess> RunCliCommandWithRetryAsync(
    ContainerlabTestCase testCase,
    string command,
    TimeSpan? timeout = null
  ) {
    Exception lastException = null;

    for ( var attempt = 1; attempt <= 10; attempt++ ) {
      try {
        return RunCliCommand( testCase, command, timeout );
      }
      catch ( Exception exception ) {
        lastException = exception;
        Log.Debug( "CLI command '{Command}' failed on attempt {Attempt}; retrying", command, attempt );
        await Task.Delay( TimeSpan.FromSeconds( 1 ) );
      }
    }

    throw new InvalidOperationException( $"CLI command '{command}' did not succeed", lastException );
  }

  private static void AssertScanSnapshot( ContainerlabTestCase testCase, IEnumerable<string> outputLines ) {
    var actual = string.Join( System.Environment.NewLine, outputLines ).Trim();
    actual = Regex.Replace( actual, "(?i)\\b[0-9a-f]{2}([-:][0-9a-f]{2}){5}\\b", "<mac>" );

    var snapshotFile = Paths.ContainerlabsDirectory / testCase.SnapshotFile;
    if ( !File.Exists( snapshotFile ) ) {
      throw new FileNotFoundException( "Scan snapshot not found", snapshotFile );
    }

    var expected = File.ReadAllText( snapshotFile ).Trim();
    if ( !string.Equals( actual, expected, StringComparison.Ordinal ) ) {
      Log.Error(
        "Scan output did not match snapshot for '{Name}'.\nActual:\n{Actual}\nExpected:\n{Expected}",
        testCase.Name, actual, expected
      );
      throw new Exception( $"Scan output did not match snapshot for '{testCase.Name}'" );
    }

    Log.Information( "Scan output matched snapshot for '{Name}'", testCase.Name );
  }

  private static void ClabLogger( OutputType type, string text ) => Log.Debug( text );

  private static IProcess Clab(
    string args,
    AbsolutePath workDir = null,
    TimeSpan? timeout = null,
    bool logOutput = true
  ) =>
    ProcessTasks.StartProcess(
      "containerlab", args,
      workingDirectory: workDir,
      timeout: (int?) timeout?.TotalMilliseconds,
      logOutput: logOutput,
      logger: logOutput ? ClabLogger : null
    );

  private static IProcess Docker(
    string args,
    AbsolutePath workDir = null,
    TimeSpan? timeout = null
  ) =>
    ProcessTasks.StartProcess(
      "docker", args,
      workingDirectory: workDir,
      timeout: (int?) timeout?.TotalMilliseconds
    );
}

/// <summary>
/// A Containerlab-based E2E test case.
/// </summary>
/// <param name="Name">Test case name</param>
/// <param name="TopologyFile">A Containerlab topology file</param>
/// <param name="SpecFile">A Drift spec file</param>
/// <param name="CliContainer">Name of the container hosting the Drift CLI</param>
/// <param name="CoordinatorAddress">Address of the coordinator used by the CLI</param>
/// <param name="AgentIds">IDs of the agents declared in the spec and enrolled before scanning</param>
/// <param name="SnapshotFile">File containing the expected JSON scan output</param>
sealed record ContainerlabTestCase(
  string Name,
  string TopologyFile,
  string SpecFile,
  string CliContainer,
  string CoordinatorAddress,
  string[] AgentIds,
  string SnapshotFile
);