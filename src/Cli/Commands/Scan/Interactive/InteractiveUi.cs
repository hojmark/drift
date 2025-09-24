using System.Text;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Scan.Interactive.KeyMaps;
using Drift.Cli.Commands.Scan.Interactive.Models;
using Drift.Cli.Output.Abstractions;
using Drift.Cli.Output.Logging;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Extensions;
using Drift.Domain.Scan;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive;

// TODO keymaps: default, vim, emacs, etc.
// TODO themes: nocolor, default, etc.
internal class InteractiveUi : IAsyncDisposable {
  private const int RenderRefreshIntervalMs = 1000;
  private const int ScrollAmount = 1;

  private readonly IOutputManager _outputManager;
  private readonly INetworkScanner _scanner;
  private readonly ScanLayout _layout = new();

  private readonly CancellationTokenSource _running = new();

  //TODO should get IDisposable warning?
  private readonly AsyncKeyInputWatcher _inputWatcher = new();
  private readonly List<Subnet> _subnets = [];
  private readonly NetworkScanOptions _scanRequest;
  private readonly IKeyMap _keyMap;

  private Percentage _progress = Percentage.Zero;

  private readonly ILogReader _logReader;
  private readonly StringBuilder _logBuilder = new();
  private TaskCompletionSource _logUpdateSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

  private TaskCompletionSource _restartSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

  public InteractiveUi(
    IOutputManager outputManager,
    INetworkScanner scanner,
    NetworkScanOptions scanRequest,
    IKeyMap keyMap
  ) {
    _scanner = scanner;
    _scanRequest = scanRequest;
    _keyMap = keyMap;
    _outputManager = outputManager;
    _scanner.ResultUpdated += OnScanResultUpdated;

    _logReader = new LogReader( _outputManager );
    _logReader.LogUpdated += OnLogUpdated;

    SubnetView = new SubnetView( () => _layout.AvailableRows );
  }

  private SubnetView SubnetView {
    get;
    set;
  }

  public async Task<int> RunAsync() {
    _ = StartScanAsync();

    await _logReader.StartAsync( _running.Token );

    await AnsiConsole
      .Live( _layout.Renderable )
      .AutoClear( true )
      .StartAsync( async ctx => {
          while ( !_running.IsCancellationRequested ) {
            var delayTask = Task.Delay( RenderRefreshIntervalMs );
            var keyTask = _inputWatcher.WaitForNextKeyAsync();
            var logTask = _logUpdateSignal.Task ?? Task.Delay( -1 );
            var restartTask = _restartSignal.Task ?? Task.Delay( -1 );

            await Task.WhenAny( delayTask, keyTask, logTask, restartTask );

            ProcessInput();

            await RenderAsync();

            ctx.Refresh();
          }
        }
      );

    return ExitCodes.Success;
  }

  private void ProcessInput() {
    var key = _inputWatcher.Consume();
    if ( key != null ) {
      var action = _keyMap.Map( key.Value );
      Handle( action );
    }
  }

  private Task<NetworkScanResult> StartScanAsync() {
    return _scanner.ScanAsync( _scanRequest, _outputManager.GetLogger(), _running.Token );
  }

  private void OnLogUpdated( object? sender, string line ) {
    lock ( _logBuilder ) {
      if ( _logBuilder.Length > 0 )
        _logBuilder.Append( '\n' );
      _logBuilder.Append( line );
      _logUpdateSignal.TrySetResult();
      _logUpdateSignal = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
    }
  }

  private async Task RenderAsync() {
    lock ( _logBuilder ) {
      _layout.SetLog( _logBuilder.ToString() );
    }

    SubnetView.Subnets = _subnets;
    _layout.SetDebug( SubnetView.DebugData );
    _layout.SetScanTree( SubnetView );
    _layout.SetProgress( _progress );
  }

  private void Handle( UiAction action ) {
    switch ( action ) {
      case UiAction.Quit:
        _running.Cancel();
        break;
      case UiAction.ScrollUp:
        SubnetView.ScrollOffset -= ScrollAmount;
        break;
      case UiAction.ScrollDown:
        SubnetView.ScrollOffset += ScrollAmount;
        break;
      case UiAction.ScrollUpPage:
        SubnetView.ScrollOffset -= (int) _layout.AvailableRows;
        break;
      case UiAction.ScrollDownPage:
        SubnetView.ScrollOffset += (int) _layout.AvailableRows;
        break;
      case UiAction.MoveUp:
        SubnetView.SelectPrevious();
        break;
      case UiAction.MoveDown:
        SubnetView.SelectNext();
        break;
      case UiAction.ToggleSubnet:
        SubnetView.ToggleSelected();
        break;
      case UiAction.RestartScan:
        _subnets.Clear();
        StartScanAsync();
        //TODO wrap below two statements in lock
        _restartSignal.SetResult();
        _restartSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        break;
      case UiAction.ToggleLog:
        _layout.ShowLogs = !_layout.ShowLogs;
        break;
      case UiAction.ToggleDebug:
        _layout.ShowDebug = !_layout.ShowDebug;
        break;
      case UiAction.None:
      default:
        break;
    }
  }

  private void OnScanResultUpdated( object? sender, NetworkScanResult scanResult ) {
    lock ( _subnets ) {
      _progress = scanResult.Progress;

      var currentSubnets = scanResult.Subnets
        .Select( subnet => new Subnet {
          Cidr = subnet.CidrBlock,
          Devices = subnet.DiscoveredDevices.Select( device =>
            new Device {
              Ip = device.Get( AddressType.IpV4 ) ?? "n/a",
              Mac = device.Get( AddressType.Mac ) ?? "n/a",
              IsOnline = true
            } ).ToList()
        } ).ToList();

      var existing = _subnets.ToDictionary( s => s.Cidr );
      var updated = new List<Subnet>();

      foreach ( var subnet in currentSubnets ) {
        if ( existing.TryGetValue( subnet.Cidr, out var existingSubnet ) ) {
          subnet.IsExpanded = existingSubnet.IsExpanded;
        }

        updated.Add( subnet );
      }

      _subnets.Clear();
      _subnets.AddRange( updated );
    }
  }

  public async ValueTask DisposeAsync() {
    _scanner.ResultUpdated -= OnScanResultUpdated;
    _running.Dispose();
    await _inputWatcher.DisposeAsync();
    _logReader.LogUpdated -= OnLogUpdated;
  }
}