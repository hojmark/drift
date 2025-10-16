using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Drift.Build.Utilities;
using Drift.Build.Utilities.ContainerImage;
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
  Target Test => _ => _
    .DependsOn( TestSelf, TestUnit, TestE2E );

  Target TestSelf => _ => _
    .Before( Version )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(TestSelf) );

        DotNetRun( s => s
          .SetProjectFile( Solution.Build.Build_Utilities_Tests.Path )
          .SetConfiguration( Configuration )
          // .ConfigureLoggers( MsBuildVerbosityParsed )
          // .SetBlameHangTimeout( "60s" )
          .AddProcessAdditionalArguments( "--disable-logo" )
          .AddProcessAdditionalArguments( "--minimum-expected-tests 18" )
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
    .DependsOn( PublishBinaries, PublishContainer )
    .After( TestUnit )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(TestE2E) );

        var version = await Versioning.Value.GetVersionAsync();

        foreach ( var runtime in SupportedRuntimes ) {
          var driftBinary = Paths.PublishDirectoryForRuntime( runtime ) / "drift";

          var envVars = new Dictionary<string, string> {
            // { nameof(EnvVar.DRIFT_BINARY_PATH), driftBinary },
            { "DRIFT_BINARY_PATH", driftBinary },
            // TODO use this!
            // { "DRIFT_CONTAINER_IMAGE_REF", ImageReference.Localhost( "drift", version ).ToString() }
            { "DRIFT_CONTAINER_IMAGE_REF", ImageReference.Localhost( "drift", version ).ToString() }
          };

          var alternateDockerHost = await FindAlternateDockerHostAsync();

          DotNetTest( settings => {
            if ( alternateDockerHost != null ) {
              Log.Information( "Using alternate Docker host: {Host}", alternateDockerHost );
              settings.SetProcessEnvironmentVariable( "DOCKER_HOST", alternateDockerHost );
            }

            return settings
              .SetProjectFile( Solution.Cli_E2ETests )
              .SetConfiguration( Configuration )
              .ConfigureLoggers( MsBuildVerbosityParsed )
              .SetBlameHangTimeout( "60s" )
              .EnableNoLogo()
              .EnableNoRestore()
              .EnableNoBuild()
              .AddProcessEnvironmentVariables( envVars );
          } );

          Log.Information( "Running E2E test on {Runtime} using binary {Binary}", runtime, driftBinary );
        }
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