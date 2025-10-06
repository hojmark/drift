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
      .Select(
        // Remove the leading "    4>" part
        line => Regex.Replace( line, @"^\s*\d+>", "" )
      )
      .ToArray();
  }
}