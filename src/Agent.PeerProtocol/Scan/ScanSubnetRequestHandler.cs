using Drift.Domain;
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

    var request = converter.FromEnvelope<ScanSubnetRequest>( envelope );
    var options = new SubnetScanOptions { Cidr = request.Cidr, PingsPerSecond = request.PingsPerSecond };

    logger.LogInformation( "Starting scan of {Cidr}", request.Cidr );

    var scanner = subnetScannerFactory.Get( request.Cidr );
    var policy = new ProgressUpdatePolicy( stream, converter, envelope, request.Cidr, logger );

    scanner.ResultUpdated += policy.Handle;

    try {
      var result = await scanner.ScanAsync( options, logger, cancellationToken );

      logger.LogInformation(
        "Scan complete for {Cidr}: {DeviceCount} devices found",
        request.Cidr,
        result.DiscoveredDevices.Count
      );

      var completeResponse = new ScanSubnetCompleteResponse { Result = result };
      await stream.SendResponseAsync( converter, completeResponse, envelope.CorrelationId );
    }
    finally {
      scanner.ResultUpdated -= policy.Handle;
    }
  }

  private sealed class ProgressUpdatePolicy {
    private readonly IPeerStream _stream;
    private readonly IPeerMessageEnvelopeConverter _converter;
    private readonly PeerMessage _envelope;
    private readonly CidrBlock _cidr;
    private readonly ILogger _logger;

    private byte _lastProgressPercentage;
    private uint _lastDeviceCount;
    private DateTime _lastSentAt = DateTime.UtcNow;

    public EventHandler<SubnetScanResult> Handle {
      get;
    }

    public ProgressUpdatePolicy(
      IPeerStream stream,
      IPeerMessageEnvelopeConverter converter,
      PeerMessage envelope,
      CidrBlock cidr,
      ILogger logger
    ) {
      _stream = stream;
      _converter = converter;
      _envelope = envelope;
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

      var progressUpdate = new ScanSubnetProgressUpdate {
        ProgressPercentage = progressPercentage, DevicesFound = deviceCount, Status = result.Status.ToString()
      };

      _stream.SendResponseFireAndForget( _converter, progressUpdate, _envelope.CorrelationId );

      _logger.LogDebug(
        "Sent progress update: {Progress}% for {Cidr}",
        progressPercentage,
        _cidr
      );
    }
  }
}