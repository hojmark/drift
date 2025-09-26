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
// TODO themes: nocolor i.e. black/white, monochrome, default, etc.
internal class InteractiveUi : IAsyncDisposable {
  private const int ScrollAmount = 1;

  private readonly IOutputManager _outputManager;
  private readonly INetworkScanner _scanner;
  private readonly ScanLayout _layout = new();

  private readonly CancellationTokenSource _running = new();

  //TODO should get IDisposable warning?
  private readonly KeyWatcher _keyWatcher = new();
  private readonly ConsoleResizeWatcher _resizeWatcher = new();

  private readonly List<Subnet> _subnets = [];
  private readonly NetworkScanOptions _scanRequest;
  private readonly IKeyMap _keyMap;

  private Percentage _progress = Percentage.Zero;

  private readonly ILogReader _logReader;
  private TaskCompletionSource _logUpdateSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
  private readonly LogView _logView;

  private TaskCompletionSource _scanUpdateSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

  public InteractiveUi(
    IOutputManager outputManager,
    INetworkScanner scanner,
    NetworkScanOptions scanRequest,
    IKeyMap keyMap,
    bool initialLogExpansion
  ) {
    _scanner = scanner;
    _scanRequest = scanRequest;
    _keyMap = keyMap;
    _outputManager = outputManager;
    _scanner.ResultUpdated += OnScanResultUpdated;

    _logReader = new LogReader( _outputManager );
    _logReader.LogUpdated += OnLogUpdated;

    SubnetView = new SubnetView( () => _layout.AvailableRows );
    _logView = new LogView( () => _layout.AvailableRows );
    _layout.ShowLogs = initialLogExpansion;
  }

  private SubnetView SubnetView {
    get;
    set;
  }

  public async Task<int> RunAsync() {
    await AnsiConsole
      .Live( _layout.Renderable )
      .AutoClear( true )
      .StartAsync( async ctx => {
          await RenderAsync();
          ctx.Refresh();

          // TODO Async scan and log reading started using different patterns
          _ = StartScanAsync();
          await _logReader.StartAsync( _running.Token );

          while ( !_running.IsCancellationRequested ) {
            var keyTask = _keyWatcher.WaitForKeyAsync();
            var resizeTask = _resizeWatcher.WaitForResizeAsync();
            var logTask = _logUpdateSignal.Task;
            var scanTask = _scanUpdateSignal.Task;

            await Task.WhenAny( keyTask, logTask, scanTask, resizeTask );

            ProcessInput();
            await RenderAsync();
            ctx.Refresh();
          }
        }
      );

    return ExitCodes.Success;
  }

  private void ProcessInput() {
    var key = _keyWatcher.Consume();
    if ( key != null ) {
      var action = _keyMap.Map( key.Value );
      Handle( action );
    }
  }

  private Task<NetworkScanResult> StartScanAsync() {
    return _scanner.ScanAsync( _scanRequest, _outputManager.GetLogger(), _running.Token );
  }

  private void OnLogUpdated( object? sender, string line ) {
    _logView.AddLine( line );
    _logUpdateSignal.TrySetResult();
    _logUpdateSignal = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
  }

  private async Task RenderAsync() {
    SubnetView.Subnets = _subnets;
    _layout.SetDebug( SubnetView.DebugData );
    _layout.SetLog( _logView );
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
        //TODO should be separately scrollable
        _logView.ScrollOffset -= ScrollAmount;
        break;
      case UiAction.ScrollDown:
        SubnetView.ScrollOffset += ScrollAmount;
        //TODO should be separately scrollable
        _logView.ScrollOffset += ScrollAmount;
        break;
      case UiAction.ScrollUpPage:
        SubnetView.ScrollOffset -= (int) _layout.AvailableRows;
        _logView.ScrollOffset -= (int) _layout.AvailableRows;
        break;
      case UiAction.ScrollDownPage:
        SubnetView.ScrollOffset += (int) _layout.AvailableRows;
        _logView.ScrollOffset += (int) _layout.AvailableRows;
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
      _scanUpdateSignal.TrySetResult();
      _scanUpdateSignal = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
    }
  }

  public async ValueTask DisposeAsync() {
    _scanner.ResultUpdated -= OnScanResultUpdated;
    _running.Dispose();
    await _keyWatcher.DisposeAsync();
    _logReader.LogUpdated -= OnLogUpdated;
  }
}