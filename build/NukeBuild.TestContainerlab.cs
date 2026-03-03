using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Drift.Build.Utilities;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Serilog;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

sealed partial class NukeBuild {
  [Parameter( "Skip containerlab deployment (useful for debugging when topology is already running)" )]
  readonly bool SkipClabDeploy = false;

  [Parameter( "Keep containerlab topology running after tests" )]
  readonly bool KeepClabRunning = false;

  [Parameter( "Run only this topology (e.g. 'simple-test'). Runs all topologies if not specified." )]
  readonly string ClabTopology = null;

  /// <summary>
  /// Defines all containerlab integration test cases.
  /// Each test case specifies a topology, its spec file, the CLI container name,
  /// and assertions to validate the scan output.
  /// </summary>
  private static readonly ContainerlabTestCase[] TestCases = [
    new(
      Name: "simple-test",
      TopologyFile: "simple-test.clab.yaml",
      SpecFile: "simple-test-spec.yaml",
      CliContainer: "clab-drift-simple-test-cli",
      Assertions: [
        new ScanAssertion( "Management subnet scanned", output => output.Contains( "172.20.20.0/24" ) ),
        new ScanAssertion( "Both scans successful (local + agent)",
          output => output.Contains( "2/2 scan operations successful" ) ),
        new ScanAssertion( "Scan completed successfully", output => output.Contains( "Distributed scan completed" ) ),
      ]
    ),
    new(
      Name: "cooperation-test",
      TopologyFile: "cooperation-test.clab.yaml",
      SpecFile: "cooperation-test-spec.yaml",
      CliContainer: "clab-drift-cooperation-test-cli",
      Assertions: [
        new ScanAssertion( "Management subnet scanned", output => output.Contains( "172.20.20.0/24" ) ),
        new ScanAssertion( "All 4 scans successful (local + 3 agents)",
          output => output.Contains( "4/4 scan operations successful" ) ),
        new ScanAssertion( "Scan completed successfully", output => output.Contains( "Distributed scan completed" ) ),
      ]
    ),
    new(
      Name: "subnet-isolation-test",
      TopologyFile: "subnet-isolation-test.clab.yaml",
      SpecFile: "subnet-isolation-test-spec.yaml",
      CliContainer: "clab-drift-subnet-isolation-test-cli",
      Assertions: [
        new ScanAssertion( "Subnet-A scanned", output => output.Contains( "192.168.10.0/24" ) ),
        new ScanAssertion( "Subnet-B scanned", output => output.Contains( "192.168.20.0/24" ) ),
        new ScanAssertion( "All scan operations successful",
          output => output.Contains( "7/7 scan operations successful" ) ),
        new ScanAssertion( "Scan completed successfully", output => output.Contains( "Distributed scan completed" ) ),
      ]
    ),
  ];

  Target TestContainerlab => _ => _
    .DependsOn( PublishContainer )
    .After( TestUnit )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(TestContainerlab) );

        var imageRef = _driftImageRef ?? throw new ArgumentNullException( nameof(_driftImageRef) );
        Log.Information( "Using image {ImageRef} for containerlab tests", imageRef );

        if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
          Log.Warning( "Containerlab tests require Linux. Skipping." );
          return;
        }

        if ( !await IsContainerlabAvailableAsync() ) {
          throw new Exception(
            "Containerlab does not appear to be installed or in PATH. " +
            "See https://containerlab.dev/install/ for installation instructions."
          );
        }

        var clabDir = RootDirectory / "containerlab";
        var casesToRun = SelectTestCases();

        Log.Information( "Running {Count} containerlab test case(s): {Names}",
          casesToRun.Length, string.Join( ", ", casesToRun.Select( tc => tc.Name ) ) );

        var total = casesToRun.Length;
        var passed = 0;
        var failed = 0;

        foreach ( var testCase in casesToRun ) {
          var run = passed + failed;
          Log.Information( "━━━ Test case: {Name} ({Run}/{Total}) ━━━", testCase.Name, run, total );

          if ( await RunTestCaseAsync( clabDir, testCase ) ) {
            passed++;
            Log.Information( "PASS: {Name}", testCase.Name );
          }
          else {
            failed++;
            Log.Error( "FAIL: {Name}", testCase.Name );
          }
        }

        Log.Information( "Containerlab integration tests: {Passed} passed, {Failed} failed", passed, failed );

        if ( failed > 0 ) {
          throw new Exception( $"{failed} containerlab test case(s) failed" );
        }
      }
    );

  private ContainerlabTestCase[] SelectTestCases() {
    if ( ClabTopology == null ) {
      return TestCases;
    }

    var selected = TestCases.Where( tc => tc.Name == ClabTopology ).ToArray();
    if ( !selected.Any() ) {
      throw new Exception(
        $"No test case found matching topology '{ClabTopology}'. " +
        $"Valid names: {string.Join( ", ", TestCases.Select( tc => tc.Name ) )}"
      );
    }

    return selected;
  }

  private async Task<bool> RunTestCaseAsync( AbsolutePath clabDir, ContainerlabTestCase testCase ) {
    var topoFile = clabDir / testCase.TopologyFile;
    var specFile = clabDir / testCase.SpecFile;

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
        await DeployContainerlabTopologyAsync( clabDir, testCase.TopologyFile );
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
        await DestroyContainerlabTopologyAsync( clabDir, testCase.TopologyFile );
      }
    }
  }

  private static async Task<bool> IsContainerlabAvailableAsync() {
    try {
      var versionOutput = await CommandRunner.RunAsync( "containerlab", "version" );
      Log.Debug( "\n{Version}", versionOutput.Trim() );
      return true;
    }
    catch {
      return false;
    }
  }

  private static async Task DeployContainerlabTopologyAsync( AbsolutePath clabDir, string topologyFile ) {
    Log.Information( "Deploying topology: {File}", topologyFile );

    DestroyTopologyIfExists( clabDir, topologyFile );
    EnsureClabManagementNetwork();

    Clab( $"deploy --topo {topologyFile}", clabDir, timeout: TimeSpan.FromMinutes( 5 ) )
      .AssertZeroExitCode();

    Log.Information( "Waiting for containers to be ready..." );
    await Task.Delay( TimeSpan.FromSeconds( 10 ) );
  }

  private static void DestroyTopologyIfExists( AbsolutePath clabDir, string topologyFile ) {
    try {
      Clab( $"destroy --topo {topologyFile} --cleanup", clabDir, timeout: TimeSpan.FromMinutes( 2 ), logOutput: false )
        .AssertZeroExitCode();
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
  /// containerlab skips the creation step and reuses it — avoiding the fatal lookup.
  ///
  /// Strategy: try to remove any stale 'clab' network (ignore failure — may be in
  /// use by another running topology), then create it. Ignore "already exists" errors
  /// from create — the important thing is the network is present before deploy.
  /// </summary>
  private static void EnsureClabManagementNetwork() {
    Log.Debug( "Pre-creating containerlab management network..." );

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

  private static async Task DestroyContainerlabTopologyAsync( AbsolutePath clabDir, string topologyFile ) {
    Log.Information( "Destroying topology: {File}", topologyFile );
    try {
      Clab( $"destroy --topo {topologyFile} --cleanup", clabDir, timeout: TimeSpan.FromMinutes( 2 ) )
        .AssertZeroExitCode();
    }
    catch ( Exception ex ) {
      Log.Warning( "Failed to destroy topology: {Error}", ex.Message );
    }
  }

  private static async Task RunScanAndAssertAsync( AbsolutePath specFile, ContainerlabTestCase testCase ) {
    Log.Information( "Running scan for test case: {Name}", testCase.Name );

    // Give agent(s) a moment to finish starting up
    await Task.Delay( TimeSpan.FromSeconds( 5 ) );

    Log.Debug( "Copying spec to CLI container {Container}...", testCase.CliContainer );
    Docker( $"cp {specFile} {testCase.CliContainer}:/tmp/spec.yaml" ).AssertZeroExitCode();

    Log.Information( "Running scan in {Container}...", testCase.CliContainer );
    var scanResult = Docker(
      $"exec {testCase.CliContainer} /app/drift scan /tmp/spec.yaml",
      timeout: TimeSpan.FromMinutes( 5 )
    );

    foreach ( var line in scanResult.Output ) {
      Log.Debug( "[scan:{Name}] {Line}", testCase.Name, line.Text );
    }

    scanResult.AssertZeroExitCode();

    AssertScanOutput( testCase, scanResult.Output.Select( o => o.Text ) );
  }

  private static void AssertScanOutput( ContainerlabTestCase testCase, IEnumerable<string> outputLines ) {
    var output = string.Join( "\n", outputLines );
    var failures = new List<string>();

    foreach ( var assertion in testCase.Assertions ) {
      if ( assertion.Check( output ) ) {
        Log.Debug( "Assertion passed: {Description}", assertion.Description );
      }
      else {
        failures.Add( assertion.Description );
        Log.Error( "Assertion failed: {Description}", assertion.Description );
      }
    }

    if ( failures.Count > 0 ) {
      Log.Error( "Scan output was:\n{Output}", output );
      var failList = string.Join( "\n", failures.Select( f => $"  FAIL: {f}" ) );
      throw new Exception( $"Scan assertions failed for '{testCase.Name}':\n{failList}" );
    }

    Log.Information( "All {Count} assertions passed for '{Name}'", testCase.Assertions.Length, testCase.Name );
  }

  // ── Process helpers ────────────────────────────────────────────────────────

  private static IProcess Clab( string args, AbsolutePath workDir = null, TimeSpan? timeout = null, bool logOutput = true ) =>
    ProcessTasks.StartProcess(
      "containerlab", args,
      workingDirectory: workDir,
      timeout: (int?) timeout?.TotalMilliseconds,
      logOutput: logOutput
    );

  private static IProcess Docker( string args, AbsolutePath workDir = null, TimeSpan? timeout = null ) =>
    ProcessTasks.StartProcess(
      "docker", args,
      workingDirectory: workDir,
      timeout: (int?) timeout?.TotalMilliseconds
    );
}

/// <summary>A containerlab integration test case.</summary>
sealed record ContainerlabTestCase(
  string Name,
  string TopologyFile,
  string SpecFile,
  string CliContainer,
  ScanAssertion[] Assertions
);

/// <summary>A named assertion over scan output text.</summary>
sealed record ScanAssertion( string Description, Func<string, bool> Check );