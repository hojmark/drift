/*using Drift.Cli.Output.Abstractions;
using Drift.Cli.Tools;
using Drift.Domain;
using Drift.Domain.Progress;
using Drift.Domain.Scan;
using Drift.Parsers.NmapXml;
using Drift.Parsers.NmapXml.Progress;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Scan.Deprecated;

// TODO default to memory stream, with disk option (dev perhaps?)
[Obsolete( $"Use {nameof(NewNetworkScanner)} instead" )]
internal class NmapNetworkScanner( IOutputManager output ) : INetworkScanner {
  public async Task<ScanResult> ScanAsync(
    Cidr cidr,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default
  ) {
    var logger = output.Log;
    var startedAt = DateTime.Now;
    logger.LogDebug( "Starting scan at {StartedAt}", startedAt );

    // Subfolder in tmp due to : https://github.com/nmap/nmap/issues/2621
    //Directory.CreateDirectory( "/home/hojmark/.drift/scans" );
    //var nmapRunOutputFile = Path.Combine( "/home/hojmark/.drift/scans", $"scan-{Guid.NewGuid()}.xml" );

    var nmapRunOutputFile = Path.GetTempFileName();

    logger.LogDebug( "Writing to temp file: {FilePath}", nmapRunOutputFile );

    var progressCts = new CancellationTokenSource();

    // using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken, progressCts.Token );
    logger.LogDebug( "Starting progress reading" );

    var progressTask = onProgress == null
      ? Task.CompletedTask
      : Task.Run( async () => {
          try {
            //NmapProgressReader.ReadProgress( nmapRunOutputFile, progressCts.Token );
            await foreach ( var progress in
                           NmapProgressReader.ReadProgressAsync( nmapRunOutputFile, progressCts.Token, logger ) ) {
              onProgress( progress );
            }
          }
          catch ( OperationCanceledException ) {
            logger.LogDebug( "Progress reading cancellation requested. Performing final read." );

            // Perform final read, as the final progress may not have been read yet assuming the cancellation
            // is requested due to nmap having exited just recently.
            var progress =
              await NmapProgressReader.ReadProgressOnceAsync( nmapRunOutputFile, CancellationToken.None, logger );
            onProgress( progress );
          }
          catch ( Exception e ) {
            Console.WriteLine( e );
          }
        },
        progressCts.Token );

    // TODO check --no-stylesheet option again for relevance
    // Load: T3 is default, trying out T5
    var nmapResult = await Nmap.RunAsync( $"{cidr} -v -oX {nmapRunOutputFile} --stats-every 50ms -T5", logCommand: true,
      logger );

    if ( nmapResult.ExitCode == 0 ) {
      logger.LogDebug( "nmap exited successfully" );
    }
    else {
      logger.LogError( "nmap exited unsuccessfully (exit code: {ExitCode})", nmapResult.ExitCode );
    }

    logger.LogDebug( "Cancelling progress reading..." );
    //TODO is the below two lines correct?
    await progressCts.CancelAsync();
    await progressTask;
    logger.LogDebug( "Progress reading task stopped" );

    if ( nmapResult.ExitCode != 0 ) {
      // log: throw new Exception( $"Nmap failed with exit code {result.ExitCode}" );
      return new ScanResult {
        Status = ScanResultStatus.Error, Metadata = new Metadata { StartedAt = startedAt, EndedAt = DateTime.Now }
      };
    }

    var nmapRun = NmapXmlReader.Deserialize( nmapRunOutputFile );

    var devices = NmapConverter.ToDevices( nmapRun );

    return new ScanResult {
      Status = ScanResultStatus.Success,
      DiscoveredDevices = devices,
      Metadata = new Metadata { StartedAt = startedAt, EndedAt = DateTime.Now }
    };
  }
}*/

