using Drift.Domain.Scan;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Scanning.Scanners;
using Microsoft.Extensions.Logging;

namespace Drift.Agent.PeerProtocol.Scan;

internal sealed class ScanSubnetRequestHandler(
  ISubnetScannerFactory subnetScannerFactory,
  ILogger logger
) : IPeerMessageHandler {
  public string MessageType => ScanSubnetRequest.MessageType;

  public async Task HandleAsync(
    PeerMessage envelope,
    IPeerMessageEnvelopeConverter converter,
    IPeerStream stream,
    CancellationToken cancellationToken
  ) {
    logger.LogInformation( "Handling scan subnet request" );

    // Deserialize request
    var request = converter.FromEnvelope<ScanSubnetRequest>( envelope );
    var options = new SubnetScanOptions { Cidr = request.Cidr, PingsPerSecond = request.PingsPerSecond };

    logger.LogInformation( "Starting scan of {Cidr}", request.Cidr );

    // Create scanner and subscribe to progress
    var scanner = subnetScannerFactory.Get( request.Cidr );
    var lastProgressPercentage = (byte) 0;

    void ProgressHandler( object? sender, SubnetScanResult result ) {
      var progressPercentage = result.Progress.Value;

      // Send progress update every 5%
      if ( progressPercentage >= lastProgressPercentage + 5 ||
           ( progressPercentage == 100 && lastProgressPercentage < 100 )
         ) {
        lastProgressPercentage = progressPercentage;

        var progressUpdate = new ScanSubnetProgressUpdate {
          ProgressPercentage = progressPercentage,
          DevicesFound = result.DiscoveredDevices.Count,
          Status = result.Status.ToString()
        };

        // Fire and forget - don't await to avoid blocking scan
        stream.SendResponseFireAndForget( converter, progressUpdate, envelope.CorrelationId );

        logger.LogDebug(
          "Sent progress update: {Progress}% for {Cidr}",
          progressPercentage,
          request.Cidr
        );
      }
    }

    scanner.ResultUpdated += ProgressHandler;

    try {
      // Execute the scan
      var result = await scanner.ScanAsync( options, logger, cancellationToken );

      logger.LogInformation(
        "Scan complete for {Cidr}: {DeviceCount} devices found",
        request.Cidr,
        result.DiscoveredDevices.Count
      );

      // Send final complete response
      var completeResponse = new ScanSubnetCompleteResponse { Result = result };
      await stream.SendResponseAsync( converter, completeResponse, envelope.CorrelationId );
    }
    finally {
      scanner.ResultUpdated -= ProgressHandler;
    }
  }
}