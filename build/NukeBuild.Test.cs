using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Drift.Build.Utilities;
using Drift.Build.Utilities.MsBuild;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

sealed partial class NukeBuild {
  private string DriftBinaryName =>
    Platform == DotNetRuntimeIdentifier.linux_x64 ? "drift" :
    Platform == DotNetRuntimeIdentifier.win_x64 ? "drift.exe" :
    throw new PlatformNotSupportedException();

  Target Test => _ => _
    .DependsOn( TestSelf, TestUnit, TestE2E, TestClab );

  Target TestSelf => _ => _
    .Before( BuildInfo )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(TestSelf) );

        DotNetRun( s => s
          .SetProjectFile( Solution.Build.Build_Utilities_Tests.Path )
          .SetConfiguration( Configuration )
          // .ConfigureLoggers( MsBuildVerbosityParsed )
          .AddProcessAdditionalArguments( "--disable-logo" )
          .AddProcessAdditionalArguments( "--minimum-expected-tests 10" )
        );
      }
    );

  Target TestLocal => _ => _
    .DependsOn( Test )
    .Executes( () => {
        var result = ProcessTasks.StartProcess(
          "dotnet",
          "trx --verbosity verbose",
          workingDirectory: RootDirectory
        );
        result.AssertZeroExitCode();
      }
    );

  Target TestUnit => _ => _
    .DependsOn( Build )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(TestUnit) );

        DotNetTest( s => s
          .SetProjectFile( Solution )
          .SetConfiguration( Configuration )
          .SetFilter( "Category!=E2E" ) // Negative filter to ensure tests all tests are discovered
          .ConfigureLoggers( MsBuildVerbosityParsed )
          .SetBlameHangTimeout( "60s" )
          .EnableNoLogo()
          .EnableNoRestore()
          .EnableNoBuild()
        );
      }
    );

  Target TestE2E => _ => _
    .DependsOn( TestE2E_General, TestE2E_Binary, TestE2E_Container );

  Target TestE2E_General => _ => _
    .DependsOn( Build )
    .After( TestUnit )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(TestE2E_General) );

        Log.Information( "Running general E2E tests" );

        DotNetTest( settings => settings
          .SetProjectFile( Solution.Cli_E2ETests_General )
          .SetConfiguration( Configuration )
          .ConfigureLoggers( MsBuildVerbosityParsed )
          .SetBlameHangTimeout( "60s" )
          .EnableNoLogo()
          .EnableNoRestore()
          .EnableNoBuild()
        );
      }
    );

  Target TestE2E_Binary => _ => _
    .DependsOn( PublishBinaries )
    .After( TestUnit )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(TestE2E_Binary) );

        var driftBinary = Paths.PublishDirectoryForRuntime( Platform ) / DriftBinaryName;

        Log.Information( "Running binary E2E tests on {Runtime} using binary {Binary}", Platform, driftBinary );

        var envVars = new Dictionary<string, string> { { "DRIFT_BINARY_PATH", driftBinary }, };

        DotNetTest( settings => settings
          .SetProjectFile( Solution.Cli_E2ETests_Binary )
          .SetConfiguration( Configuration )
          .ConfigureLoggers( MsBuildVerbosityParsed )
          .SetBlameHangTimeout( "60s" )
          .EnableNoLogo()
          .EnableNoRestore()
          .EnableNoBuild()
          .AddProcessEnvironmentVariables( envVars )
        );
      }
    );

  Target TestE2E_Container => _ => _
    .DependsOn( PublishBinaries, BuildContainerImage )
    .After( TestUnit )
    .OnlyWhenDynamic( () => Platform != DotNetRuntimeIdentifier.win_x64 )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(TestE2E_Container) );

        var imageRef = _driftImageRef ?? throw new ArgumentNullException( nameof(_driftImageRef) );
        Log.Information( "Using image {ImageRef}", imageRef );

        var driftBinary = Paths.PublishDirectoryForRuntime( Platform ) / DriftBinaryName;

        Log.Information( "Running container E2E tests on {Runtime} using binary {Binary}", Platform, driftBinary );
        Log.Debug( "Supported runtimes are {SupportedRuntimes}", string.Join( ", ", SupportedRuntimes ) );

        var envVars = new Dictionary<string, string> {
          { "DRIFT_BINARY_PATH", driftBinary }, { "DRIFT_CONTAINER_IMAGE_REF", imageRef.ToString() }
        };

        var alternateDockerHost = await FindAlternateDockerHostAsync();

        DotNetTest( settings => {
          if ( alternateDockerHost != null ) {
            Log.Information( "Using alternate Docker host: {Host}", alternateDockerHost );
            settings.SetProcessEnvironmentVariable( "DOCKER_HOST", alternateDockerHost );
          }

          return settings
            .SetProjectFile( Solution.Cli_E2ETests_Container )
            .SetConfiguration( Configuration )
            .ConfigureLoggers( MsBuildVerbosityParsed )
            .SetBlameHangTimeout( "60s" )
            .EnableNoLogo()
            .EnableNoRestore()
            .EnableNoBuild()
            .AddProcessEnvironmentVariables( envVars );
        } );
      }
    );

  [ItemCanBeNull]
  private static async Task<string> FindAlternateDockerHostAsync() {
    if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
      Log.Debug( "Not running on Linux, skipping alternate Docker host search" );
      return null;
    }

    Log.Debug( "Looking for alternate Docker host..." );

    if ( await IsPodmanAvailableAsync() ) {
      var output = await CommandRunner.RunAsync( "podman", "info --format '{{.Host.RemoteSocket.Path}}'" );
      var host = "unix://" + output.Trim().Trim( '\'' );
      Log.Debug( "Found Podman at {SocketPath}", host );
      return host;
    }

    Log.Debug( "Found no Docker host alternative" );

    return null;
  }

  private static async Task<bool> IsPodmanAvailableAsync() {
    try {
      var versionOutput = await CommandRunner.RunAsync( "podman", "--version" );
      return versionOutput.StartsWith( "podman", StringComparison.OrdinalIgnoreCase );
    }
    catch {
      return false;
    }
  }
}