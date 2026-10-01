using Drift.Domain.Scan;
using Drift.Messaging.Protocol.Agent.Scan;
using Drift.Networking.Core.Abstractions;
using Drift.Scanning.Scanners.Factories;
using Microsoft.Extensions.Logging;

namespace Drift.Agent.Host.Scan;

internal sealed class ScanSubnetRequestHandler(
  ISubnetScannerFactory subnetScannerFactory,
  ILogger logger
) : StreamingRequestHandler<ScanSubnetRequest, ScanSubnetProgress, ScanSubnetResponse>( logger ) {
  public override async Task HandleAsync(
    ScanSubnetRequest request,
    IStreamingMessageResponder<ScanSubnetProgress, ScanSubnetResponse> responder,
    CancellationToken cancellationToken
  ) {
    var options = new SubnetScanOptions { Cidr = request.Cidr, PingsPerSecond = request.PingsPerSecond };

    Logger.LogInformation( "Starting scan of {Cidr}", request.Cidr );

    var subnetScanner = subnetScannerFactory.Get( request.Cidr );
    var policy = new ProgressUpdatePolicy( responder, request.Cidr, Logger );

    subnetScanner.ResultUpdated += policy.Handle;

    try {
      var result = await subnetScanner.ScanAsync( options, Logger, cancellationToken );

      Logger.LogInformation(
        "Scan complete for {Cidr}: {DeviceCount} devices found",
        request.Cidr,
        result.DiscoveredDevices.Count
      );

      var completeResponse = new ScanSubnetResponse { Result = result };
      await responder.SendAsync( completeResponse );
    }
    finally {
      subnetScanner.ResultUpdated -= policy.Handle;
    }
  }
}