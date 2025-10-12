using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Drift.Build.Utilities.ContainerImage;
using Drift.Cli.E2ETests.Abstractions;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
using Utilities;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

sealed partial class NukeBuild {
  Target Test => _ => _
    .DependsOn( TestUnit, TestE2E );

  Target TestLocal => _ => _
    .DependsOn( Test )
    .Executes( () => {
        // DotNetToolRestore();
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

        // TODO
        foreach ( var runtime in SupportedRuntimes ) {
          var driftBinary = Paths.PublishDirectoryForRuntime( runtime ) / "drift";

          var envVars = new Dictionary<string, string> {
            { nameof(EnvVar.DRIFT_BINARY_PATH), driftBinary },
            // TODO use this!
            { nameof(EnvVar.DRIFT_CONTAINER_IMAGE_TAG), ImageReference.Localhost( "drift", SemVer ).ToString() }
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
              .AddProcessEnvironmentVariables( envVars )
              .ConfigureLoggers( MsBuildVerbosityParsed )
              .EnableNoLogo()
              .EnableNoRestore()
              .EnableNoBuild();
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
      using var process = Process.Start( new ProcessStartInfo {
        FileName = "podman",
        Arguments = "info --format '{{.Host.RemoteSocket.Path}}'",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
      } );

      await process.WaitForExitAsync();

      string output = await process.StandardOutput.ReadToEndAsync();

      var host = "unix://" + output.Trim().Trim( '\'' );

      Log.Debug( "Found Podman at {SocketPath}", host );
    }

    Log.Debug( "Found no Docker host alternative" );

    return null;
  }

  private static async Task<bool> IsPodmanAvailableAsync() {
    try {
      var versionOutput = await RunCommandAsync( "podman", "--version" );
      return versionOutput.StartsWith( "podman", StringComparison.OrdinalIgnoreCase );
    }
    catch {
      return false;
    }
  }

  static async Task<string> RunCommandAsync( string command, string arguments ) {
    using var process = Process.Start( new ProcessStartInfo {
      FileName = command,
      Arguments = arguments,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    } );

    await process.WaitForExitAsync();

    string output = process.StandardOutput.ReadToEnd();
    string error = process.StandardError.ReadToEnd();

    if ( process.ExitCode != 0 ) {
      throw new Exception( $"Command '{command} {arguments}' failed:\n{error}" );
    }

    return output;
  }
}