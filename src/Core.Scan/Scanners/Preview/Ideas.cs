using Drift.Domain.Device;
using Drift.Domain.Device.Declared;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan.Scanners.Preview;

public interface INetworkSpec;

public class ScanOptions
{
  public bool CompareWithSpec { get; set; }
  public string OutputFormat { get; set; } = "table"; // "json", etc.
  public string SpecFilePath { get; set; }
}

public class DriftResult
{
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;

  // Replace with type that links declared and discovered device to allow for comparing each device for drift
  public List<IAddressableDevice> Devices { get; set; } = new();
  public List<DeclaredDevice> MissingDevices { get; set; } = new();
  public List<DiscoveredDevice> UnexpectedDevices { get; set; } = new();

  // Useful for single exit code reporting
  //public bool IsDriftDetected => Devices.DevicesWithDrift().Any() || MissingDevices.Any() || UnexpectedDevices.Any() ;
}

public interface Ideas;

public interface INetworkComparer;

public interface IDriftComparer
{
  DriftResult Compare(INetworkSpec declared, NetworkScanResult actual);
}

public class DeviceDrift
{
  public string DeviceId { get; set; } // e.g. MAC, Hostname, etc.
  public DeclaredDevice Declared { get; set; }
  public DiscoveredDevice Actual { get; set; }
  //public List<PropertyDifference> Differences { get; set; } = new();
}


public class DefaultScanOrchestrator : Ideas
{
  private readonly INetworkScanner _networkScanner;
  //private readonly INetworkSpecLoader _specLoader;
  private readonly IDriftComparer _driftComparer;
  //private readonly IEnumerable<IScanResultFormatter> _formatters;
  private readonly ILogger<DefaultScanOrchestrator> _logger;

  public DefaultScanOrchestrator(
    INetworkScanner networkScanner,
    //INetworkSpecLoader specLoader,
    IDriftComparer driftComparer,
    //IEnumerable<IScanResultFormatter> formatters,
    ILogger<DefaultScanOrchestrator> logger)
  {
    _networkScanner = networkScanner;
    //_specLoader = specLoader;
    _driftComparer = driftComparer;
    //_formatters = formatters;
    _logger = logger;
  }

  public async Task RunScanAsync(ScanOptions options)
  {
    /*_logger.LogInformation("Starting network scan...");

    var actualState = await _networkScanner.ScanAsync();

    DriftResult? drift = null;

    if (options.CompareWithSpec)
    {
      var declaredSpec = await _specLoader.LoadSpecAsync(options.SpecFilePath);
      drift = _driftComparer.Compare(declaredSpec, actualState);
    }

    var formatter = _formatters
                      .FirstOrDefault(f => f.FormatName.Equals(options.OutputFormat, StringComparison.OrdinalIgnoreCase))
                    ?? _formatters.First(f => f.FormatName == "table");

    var output = formatter.Format(actualState, drift);
    Console.WriteLine(output);*/
  }
}