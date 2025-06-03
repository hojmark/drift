using System.Globalization;
using Drift.Domain.Progress;
using Microsoft.Extensions.Logging;
using NmapTaskBegin = NmapXmlParser.taskbegin;
using NmapTaskEnd = NmapXmlParser.taskend;
using NmapTaskProgress = NmapXmlParser.taskprogress;
using NmapRun = NmapXmlParser.nmaprun;

namespace Drift.Parsers.NmapXml.Progress;

/**
 *
 Example XML:
<taskprogress task="Ping Scan" time="1745495724" percent="39.45" remaining="2" etc="1745495726"/>
<taskprogress task="Ping Scan" time="1745495725" percent="98.14" remaining="1" etc="1745495725"/>
<taskprogress task="Parallel DNS resolution of 19 hosts." time="1745495726" percent="0.00"/>
<taskprogress task="Parallel DNS resolution of 19 hosts." time="1745495727" percent="0.00"/>
<taskprogress task="Parallel DNS resolution of 19 hosts." time="1745495728" percent="0.00"/>
<taskprogress task="Parallel DNS resolution of 19 hosts." time="1745495729" percent="36.84" remaining="7" etc="1745495736"/>
<taskprogress task="Parallel DNS resolution of 19 hosts." time="1745495730" percent="52.63" remaining="5" etc="1745495735"/>
<taskprogress task="Parallel DNS resolution of 19 hosts." time="1745495731" percent="52.63" remaining="6" etc="1745495736"/>
<taskprogress task="Parallel DNS resolution of 19 hosts." time="1745495732" percent="52.63" remaining="7" etc="1745495738"/>
<taskprogress task="Parallel DNS resolution of 19 hosts." time="1745495733" percent="63.16" remaining="5" etc="1745495738"/>
<taskprogress task="Connect Scan" time="1745495734" percent="50.05" remaining="1" etc="1745495735"/>
<taskprogress task="Connect Scan" time="1745495735" percent="82.13" remaining="1" etc="1745495735"/>
<taskprogress task="Connect Scan" time="1745495736" percent="84.99" remaining="1" etc="1745495737"/>
<taskprogress task="Connect Scan" time="1745495737" percent="88.71" remaining="1" etc="1745495738"/>
<taskprogress task="Connect Scan" time="1745495738" percent="93.81" remaining="1" etc="1745495738"/>
<taskprogress task="Connect Scan" time="1745495739" percent="95.81" remaining="1" etc="1745495739"/>
<taskprogress task="Connect Scan" time="1745495740" percent="97.58" remaining="1" etc="1745495740"/>
<taskprogress task="Connect Scan" time="1745495741" percent="98.63" remaining="1" etc="1745495741"/>
<taskprogress task="Connect Scan" time="1745495742" percent="99.69" remaining="1" etc="1745495742"/>
 */
public static class NmapProgressReader {
  private static readonly TimeSpan ReadInterval = TimeSpan.FromMilliseconds( 25 );

  // Note: begin and end only available when using verbose mode
  public static async IAsyncEnumerable<ProgressReport> ReadProgressAsync(
    string filePath,
    /* [EnumeratorCancellation] */CancellationToken cancellationToken,
    ILogger? logger = null
  ) {
    while ( !cancellationToken.IsCancellationRequested ) {
      await Task.Delay( ReadInterval, cancellationToken );

      if ( !File.Exists( filePath ) ) {
        continue;
      }

      yield return await ReadProgressOnceAsync( filePath, cancellationToken, logger, ignoreErrors: true );
    }
  }

  public static async Task<ProgressReport> ReadProgressOnceAsync(
    string filePath,
    CancellationToken cancellationToken,
    ILogger? logger = null,
    bool? ignoreErrors = false
  ) {
    ProgressReport progressReport;

    //Console.WriteLine( "File exists: " + File.Exists( filePath ) );

    try {
      //TODO test for valid xml - retry fast until valid
      var copied = filePath + ".copy";
      File.Copy( filePath, copied, true );
      // Reading while nmap is writing the closing tag is missing
      if ( ( await File.ReadAllLinesAsync( copied, cancellationToken ) ).Last() != "</nmaprun>" )
        await File.AppendAllTextAsync( copied, "</nmaprun>", cancellationToken );

      var nmaprun = NmapXmlReader.Deserialize( copied );

      var taskBegin = nmaprun.GetItems<NmapTaskBegin>();
      var taskProgress = nmaprun.GetItems<NmapTaskProgress>();
      var taskEnd = nmaprun.GetItems<NmapTaskEnd>();

      var latestEnd = taskEnd
        .GroupBy( t => t.task )
        .Select( g => g
          .OrderByDescending( t =>
            double.Parse( t.time, CultureInfo.InvariantCulture ) )
          .First()
        )
        .ToList();

      var latestProgress = taskProgress
        .GroupBy( t => t.task )
        .Select( g => g
          .OrderByDescending( t =>
            double.Parse( t.time, CultureInfo.InvariantCulture ) )
          .First()
        )
        .ToList();

      var latestBegins = taskBegin
        .GroupBy( t => t.task )
        .Select( g => g
          .OrderByDescending( t =>
            double.Parse( t.time, CultureInfo.InvariantCulture ) )
          .First()
        )
        .ToList();

      var taskNames = latestEnd
        .Select( t => t.task )
        .Union( latestProgress.Select( t => t.task ) )
        .Union( latestBegins.Select( t => t.task ) )
        .ToHashSet();

      progressReport = new ProgressReport();

      foreach ( var taskName in taskNames ) {
        var end = latestEnd.SingleOrDefault( t => t.task == taskName );
        var begin = latestBegins.SingleOrDefault( t => t.task == taskName );
        var progress = latestProgress.SingleOrDefault( t => t.task == taskName );

        int pct = end != null ? 100 :
          //TODO enable analyzer warning if not invariant
          progress != null ? (int) double.Parse( progress.percent, CultureInfo.InvariantCulture ) : 0;

        progressReport.Tasks.Add( new TaskProgress { TaskName = taskName, CompletionPct = pct } );
      }

      //Console.WriteLine( progressReport );
    }
    catch ( Exception e ) {
      if ( !ignoreErrors.HasValue || !ignoreErrors.Value ) {
        logger?.LogError( e, "Unexpected exception while reading progress" );
        Console.Error.WriteLine( e );
      }

      // Add initializing (progress) task?
      progressReport = new ProgressReport(); // empty if failed parsing
    }

    return progressReport;
  }

  private static IEnumerable<T> GetItems<T>( this NmapRun nmaprun ) {
    return nmaprun.Items?.Where( i => i is T ).Cast<T>() ?? [];
  }
}