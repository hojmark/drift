using Drift.Domain;
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

  private sealed class ProgressUpdatePolicy {
    private readonly IStreamingMessageResponder<ScanSubnetProgress, ScanSubnetResponse> _responder;
    private readonly CidrBlock _cidr;
    private readonly ILogger _logger;

    private byte _lastProgressPercentage;
    private uint _lastDeviceCount;
    private DateTime _lastSentAt = DateTime.UtcNow;

    public EventHandler<SubnetScanResult> Handle {
      get;
    }

    public ProgressUpdatePolicy(
      IStreamingMessageResponder<ScanSubnetProgress, ScanSubnetResponse> responder,
      CidrBlock cidr,
      ILogger logger
    ) {
      _responder = responder;
      _cidr = cidr;
      _logger = logger;
      Handle = OnResultUpdated;
    }

    private void OnResultUpdated( object? sender, SubnetScanResult result ) {
      var progressPercentage = result.Progress.Value;
      var deviceCount = result.DiscoveredDevices.Count;
      var now = DateTime.UtcNow;

      bool progressThresholdReached = progressPercentage >= _lastProgressPercentage + 5;
      bool isFirstCompletion = progressPercentage == 100 && _lastProgressPercentage < 100;
      bool heartbeatDue = now - _lastSentAt > TimeSpan.FromSeconds( 10 );
      bool firstDeviceDiscovered = deviceCount > 0 && _lastDeviceCount == 0;

      if ( !( progressThresholdReached || isFirstCompletion || heartbeatDue || firstDeviceDiscovered ) ) {
        return;
      }

      _lastProgressPercentage = progressPercentage;
      _lastDeviceCount = (uint) deviceCount;
      _lastSentAt = now;

      var progressUpdate = new ScanSubnetProgress {
        ProgressPercentage = progressPercentage, DevicesFound = deviceCount, Status = result.Status.ToString()
      };

      _responder.SendProgress( progressUpdate );

      _logger.LogDebug(
        "Sent progress update: {Progress}% for {Cidr}",
        progressPercentage,
        _cidr
      );
    }
  }
}