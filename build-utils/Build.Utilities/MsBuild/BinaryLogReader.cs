using System.Text.RegularExpressions;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace Drift.Build.Utilities.MsBuild;

public static class BinaryLogReader {
  public static string[] GetWarnings( string binaryLogName ) {
    string warningsLogName = $"{binaryLogName}-warnings-only.log";

    DotNetMSBuild( s => s
      .SetTargetPath( binaryLogName )
      .SetNoConsoleLogger( true )
      .AddProcessAdditionalArguments( "-fl", $"-flp:logfile={warningsLogName};warningsonly" )
    );

    return File.ReadAllLines( warningsLogName )
      .Select( line =>
        // Remove the leading "    4>" part
        Regex.Replace( line, @"^\s*\d+>", string.Empty )
      )
      .ToArray();
  }
}