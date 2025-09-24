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

    SubnetViewport = new SubnetView( () => _layout.GetAvailableRows() );
  }

  private SubnetView SubnetViewport {
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
    var key = _inputWatcher.ConsumeKey();
    if ( key != null ) {
      HandleInput( key.Value );
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

    SubnetViewport.Subnets = _subnets;

    // Debug information (remove after fixing)
    _layout.SetDebug( SubnetViewport.DebugData );

    _layout.SetScanTree( SubnetViewport );

    _layout.SetProgress( _progress );
  }

  private void HandleInput( ConsoleKey key ) {
    var action = _keyMap.MapKey( key );

    switch ( action ) {
      case UiAction.Quit:
        _running.Cancel();
        break;
      case UiAction.ScrollUp:
        SubnetViewport.ScrollOffset -= ScrollAmount;
        break;
      case UiAction.ScrollDown:
        SubnetViewport.ScrollOffset += ScrollAmount;
        break;
      case UiAction.ScrollUpPage:
        SubnetViewport.ScrollOffset -= (int) _layout.GetAvailableRows();
        break;
      case UiAction.ScrollDownPage:
        SubnetViewport.ScrollOffset += (int) _layout.GetAvailableRows();
        break;
      case UiAction.MoveUp:
        SubnetViewport.SelectPrevious();
        break;
      case UiAction.MoveDown:
        SubnetViewport.SelectNext();
        break;
      case UiAction.ToggleSubnet:
        SubnetViewport.ToggleSelected();
        break;
      case UiAction.RestartScan:
        _subnets.Clear();
        StartScanAsync();
        //TODO wrap below two statements in lock
        _restartSignal?.SetResult();
        _restartSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        //_selectedIndex = 0;
        //_scrollOffset = 0;
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
        .Select( kvp => new Subnet {
          Cidr = kvp.CidrBlock,
          Devices = kvp.DiscoveredDevices.Select( dd =>
            new Device {
              Ip = dd.Get( AddressType.IpV4 ) ?? "n/a", Mac = dd.Get( AddressType.Mac ) ?? "n/a", IsOnline = true
            } ).ToList()
        } ).ToList();

      // Same logic as before, but triggered by events
      var existingSubnetsMap = _subnets.ToDictionary( s => s.Cidr );
      var updatedUiSubnets = new List<Subnet>();

      foreach ( var subnet in currentSubnets ) {
        if ( existingSubnetsMap.TryGetValue( subnet.Cidr, out var existingUiSubnet ) ) {
          subnet.IsExpanded = existingUiSubnet.IsExpanded;
          updatedUiSubnets.Add( subnet );
        }
        else {
          updatedUiSubnets.Add( subnet );
        }
      }

      _subnets.Clear();
      _subnets.AddRange( updatedUiSubnets );
    }
  }

  public async ValueTask DisposeAsync() {
    _scanner.ResultUpdated -= OnScanResultUpdated;
    _running.Dispose();
    await _inputWatcher.DisposeAsync();

    _logReader.LogUpdated -= OnLogUpdated;
  }
}