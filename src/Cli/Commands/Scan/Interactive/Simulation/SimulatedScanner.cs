using Drift.Domain.Scan;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Device.Addresses;

namespace Drift.Cli.Commands.Scan.Interactive.Simulation;

public class SimulatedScanner : IScanService, IDisposable {
  public bool IsComplete => _visibleDevices.Count >= _totalDevices;

  public uint Progress {
    get {
      if ( !_started )
        return 0;

      var elapsed = DateTime.Now - _startedAt;
      double ratio = elapsed.TotalSeconds / _duration.TotalSeconds;

      return (uint) Math.Clamp( ratio * 100, 0, 100 );
    }
  }

  public event EventHandler<List<Subnet>>? SubnetsUpdated;
  public event EventHandler<ScanResult>? ResultUpdated;

  private readonly List<Device> _allDevices;
  private readonly Dictionary<string, List<Device>> _visibleBySubnet = new();
  private readonly TimeSpan _duration;
  private readonly int _totalDevices;
  private readonly Timer _updateTimer;

  private bool _started;
  private DateTime _startedAt;
  private HashSet<Device> _visibleDevices = [];
  private readonly ScanSession _session;

  public SimulatedScanner( ScanSession session ) {
    _session = session;
    _duration = session.Duration;
    _allDevices = session.Subnets.SelectMany( s => s.Devices ).ToList();
    _totalDevices = _allDevices.Count;

    foreach ( var subnet in session.Subnets )
      _visibleBySubnet[subnet.Address] = [];

    // Create timer but don't start it yet
    _updateTimer = new Timer( OnTimerTick, null, Timeout.Infinite, Timeout.Infinite );
  }

  public void Start( CancellationToken cancellationToken = default ) {
    _visibleDevices.Clear();
    foreach ( var subnetDevices in _visibleBySubnet.Values ) {
      subnetDevices.Clear();
    }

    _started = true;
    _startedAt = DateTime.Now;

    // Fire initial empty state
    UpdateAndFireEvents();

    // Start the timer - fire every 100ms during scanning
    _updateTimer.Change( 100, 100 );
  }

  private void OnTimerTick( object? state ) {
    if ( !_started ) return;

    var elapsed = DateTime.Now - _startedAt;
    double percent = Math.Clamp( elapsed.TotalSeconds / _duration.TotalSeconds, 0, 1 );
    int devicesToShow = (int) Math.Floor( percent * _totalDevices );

    // Reveal devices in order
    bool hasChanges = false;
    foreach ( var device in _allDevices.Take( devicesToShow ) ) {
      if ( !_visibleDevices.Add( device ) )
        continue;

      hasChanges = true;
      var subnet = _session.Subnets.First( s => s.Devices.Contains( device ) );
      _visibleBySubnet[subnet.Address].Add( device );
    }

    // Fire events if there are changes or we're complete
    if ( hasChanges || percent >= 1.0 ) {
      UpdateAndFireEvents();
    }

    // Stop timer when complete
    if ( percent >= 1.0 ) {
      _updateTimer.Change( Timeout.Infinite, Timeout.Infinite );
    }
  }

  private void UpdateAndFireEvents() {
    var currentSubnets = _session.Subnets
      .Select( s => new Subnet( s.Address, [.._visibleBySubnet[s.Address]] ) )
      .ToList();

    SubnetsUpdated?.Invoke( this, currentSubnets );
    ResultUpdated?.Invoke( this,
      new ScanResult {
        Metadata = null,
        Status = ScanResultStatus.InProgress,
        DiscoveredDevices = currentSubnets.Select( s => s.Devices.Select( ConvertToDiscoveredDevice ) )
          .SelectMany( d => d )
      } );

    /*var scanResult = new ScanResult {
      DiscoveredDevices = _visibleDevices.Select(ConvertToDiscoveredDevice),
      Status = IsComplete ? ScanResultStatus.Success : ScanResultStatus.InProgress,
      Metadata = new Metadata { StartedAt = _startedAt, EndedAt = DateTime.Now },
      Progress = Progress
    };

    ResultUpdated?.Invoke(this, scanResult);*/
  }

  private static DiscoveredDevice ConvertToDiscoveredDevice( Device device ) {
    var addresses = new List<IDeviceAddress> { new IpV4Address( device.IP ) };

    if ( !string.IsNullOrWhiteSpace( device.MAC ) ) {
      addresses.Add( new MacAddress( device.MAC ) );
    }

    return new DiscoveredDevice { Addresses = addresses };
  }

  public void Dispose() {
    _updateTimer?.Dispose();
  }

  public Task<ScanResult> ScanAsync( ScanRequest request, CancellationToken cancellationToken = default ) {
    Start( cancellationToken );
    return Task.FromResult( new ScanResult { Status = ScanResultStatus.Success, Metadata = null } );
  }
}