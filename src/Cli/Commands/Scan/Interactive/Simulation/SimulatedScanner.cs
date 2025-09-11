namespace Drift.Cli.Commands.Scan.Interactive.Simulation;

public class SimulatedScanner : IScanner {
  public ScanSession Session {
    get;
  }

  public bool IsComplete => _visibleDevices.Count >= _totalDevices;

  public uint Progress {
    get {
      if ( !_started )
        return 0;

      TimeSpan elapsed = DateTime.Now - _startedAt;
      double ratio = elapsed.TotalSeconds / _duration.TotalSeconds;

      return (uint) Math.Clamp( ratio * 100, 0, 100 );
    }
  }

  private readonly List<Device> _allDevices;
  private readonly Dictionary<string, List<Device>> _visibleBySubnet = new();
  private readonly TimeSpan _duration;
  private readonly int _totalDevices;

  private bool _started = false;
  private DateTime _startedAt;
  private HashSet<Device> _visibleDevices = null!;

  public SimulatedScanner( ScanSession session ) {
    Session = session;
    _duration = session.Duration;
    _allDevices = session.Subnets.SelectMany( s => s.Devices ).ToList();
    _totalDevices = _allDevices.Count;

    foreach ( var subnet in session.Subnets )
      _visibleBySubnet[subnet.Address] = new List<Device>();
  }

  public void Start() {
    _visibleDevices = new();
    _started = true;
    _startedAt = DateTime.Now;
  }

  public List<Subnet> GetCurrentSubnets() {
    if ( !_started )
      return EmptySubnets();

    var elapsed = DateTime.Now - _startedAt;
    double percent = Math.Clamp( elapsed.TotalSeconds / _duration.TotalSeconds, 0, 1 );
    int devicesToShow = (int) Math.Floor( percent * _totalDevices );

    // Reveal devices in order
    foreach ( var device in _allDevices.Take( devicesToShow ) ) {
      if ( _visibleDevices.Contains( device ) )
        continue;

      _visibleDevices.Add( device );

      var subnet = Session.Subnets.First( s => s.Devices.Contains( device ) );
      _visibleBySubnet[subnet.Address].Add( device );
    }

    return Session.Subnets
      .Select( s => new Subnet( s.Address, new List<Device>( _visibleBySubnet[s.Address] ) ) )
      .ToList();
  }

  private List<Subnet> EmptySubnets() {
    return Session.Subnets
      .Select( s => new Subnet( s.Address, new List<Device>() ) )
      .ToList();
  }
}