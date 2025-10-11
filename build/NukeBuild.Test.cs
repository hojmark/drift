using System;
using System.Collections.Generic;
using System.Diagnostics;
using Drift.Build.Utilities.ContainerImage;
using Drift.Cli.E2ETests.Abstractions;
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
        using var _ = new TargetLifecycle( nameof(TestUnit) );

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
        using var _ = new TargetLifecycle( nameof(TestE2E) );

        string? podmanHost = null;
        if ( IsPodmanAvailable() ) {
          // Step 1: Run `podman info --format json`
          ProcessStartInfo psi = new ProcessStartInfo {
            FileName = "podman",
            Arguments = "info --format '{{.Host.RemoteSocket.Path}}'",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
          };

          using Process process = Process.Start( psi );
          string output = process.StandardOutput.ReadToEnd();
          await process.WaitForExitAsync();

          podmanHost = "unix://" + output.Trim().Trim( '\'' );
          Log.Information( "Found socket path: {SocketPath}", podmanHost );
        }
        else {
          Log.Warning( "Podman not available. " );
        }

        // TODO
        foreach ( var runtime in SupportedRuntimes ) {
          var publishDir = Paths.PublishDirectoryForRuntime( runtime );
          var driftBinary = publishDir / "drift";

          var envs = new Dictionary<string, string> {
            { nameof(EnvVar.DRIFT_BINARY_PATH), driftBinary },
            // TODO use this!
            { nameof(EnvVar.DRIFT_CONTAINER_IMAGE_TAG), ImageReference.Localhost( "drift", SemVer ).ToString() }
          };

          DotNetTest( s => {
            if ( podmanHost != null ) {
              s.SetProcessEnvironmentVariable( "DOCKER_HOST", podmanHost );
            }

            return s
              .SetProjectFile( Solution.Cli_E2ETests )
              .SetConfiguration( Configuration )
              .AddProcessEnvironmentVariables( envs )
              .ConfigureLoggers( MsBuildVerbosityParsed )
              .EnableNoLogo()
              .EnableNoRestore()
              .EnableNoBuild();
          } );

          Log.Information( "Running E2E test on {Runtime} build to {PublishDir}", runtime, publishDir );
        }
      }
    );

  static bool IsPodmanAvailable() {
    try {
      string versionOutput = RunCommand( "podman", "--version" );
      return versionOutput.StartsWith( "podman", StringComparison.OrdinalIgnoreCase );
    }
    catch {
      return false;
    }
  }

  static string RunCommand( string command, string arguments ) {
    ProcessStartInfo psi = new ProcessStartInfo {
      FileName = command,
      Arguments = arguments,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    using Process process = Process.Start( psi );
    string output = process.StandardOutput.ReadToEnd();
    string error = process.StandardError.ReadToEnd();
    process.WaitForExit();

    if ( process.ExitCode != 0 ) {
      throw new Exception( $"Command '{command} {arguments}' failed with error:\n{error}" );
    }

    return output;
  }
}