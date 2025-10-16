using Nuke.Common.Tools.DotNet;

namespace Drift.Build.Utilities.MsBuild;

public enum MsBuildVerbosity {
  Quiet,
  Minimal,
  Normal,
  Detailed,
  Diagnostic
}

public static class MsBuildVerbosityExtensions {
  public static string ToMsBuildVerbosity( this MsBuildVerbosity verbosity ) {
    return verbosity switch {
      MsBuildVerbosity.Quiet => "quiet",
      MsBuildVerbosity.Minimal => "minimal",
      MsBuildVerbosity.Normal => "normal",
      MsBuildVerbosity.Detailed => "detailed",
      MsBuildVerbosity.Diagnostic => "diagnostic",
      _ => throw new ArgumentOutOfRangeException( nameof(verbosity), verbosity, null )
    };
  }

  public static MsBuildVerbosity FromMsBuildVerbosity( string verbosity ) {
    return verbosity switch {
      "quiet" => MsBuildVerbosity.Quiet,
      "minimal" => MsBuildVerbosity.Minimal,
      "normal" => MsBuildVerbosity.Normal,
      "detailed" => MsBuildVerbosity.Detailed,
      "diagnostic" => MsBuildVerbosity.Diagnostic,
      _ => throw new ArgumentOutOfRangeException( nameof(verbosity), verbosity, null )
    };
  }

  // https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference?view=vs-2022#switches-for-loggers
  public static DotNetTestSettings ConfigureLoggers( this DotNetTestSettings settings, MsBuildVerbosity verbosity ) {
    return settings
      .AddLoggers( $"\"console;verbosity={ToMsBuildVerbosity( verbosity )}\"" )
      .AddLoggers( "trx" );
  }
}