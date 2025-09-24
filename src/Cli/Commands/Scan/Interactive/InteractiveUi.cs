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
  private readonly AsyncKeyInputWatcher _inputWatcher = new();
  private readonly List<Subnet> _subnets = [];
  private readonly NetworkScanOptions _scanRequest;
  private readonly IKeyMap _keyMap;

  private Percentage _progress = Percentage.Zero;

  private readonly bool _logEnabled = true;
  private readonly ILogReader? _logReader;
  private readonly StringBuilder _logBuilder = new();
  private TaskCompletionSource<bool>? _logUpdateSignal;


  //TODO should get IDisposable warning?

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
    if ( _logEnabled ) {
      _logReader = new LogReader( _outputManager );
      _logReader.LogUpdated += OnLogUpdated;
    }

    int availableRows = _layout.GetAvailableRows();
    SubnetViewPort = new SubnetViewPort( (uint) availableRows );
  }

  private SubnetViewPort SubnetViewPort {
    get;
    set;
  }

  public async Task<int> RunAsync() {
    _ = StartScanAsync();

    if ( _logEnabled && _logReader != null ) {
      await _logReader.StartAsync( _running.Token );
    }

    await AnsiConsole
      .Live( _layout.Renderable )
      .AutoClear( true )
      .StartAsync( async ctx => {
          while ( !_running.IsCancellationRequested ) {
            _logUpdateSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

            var delayTask = Task.Delay( RenderRefreshIntervalMs );
            var keyTask = _inputWatcher.WaitForNextKeyAsync();
            var logTask = _logEnabled ? _logUpdateSignal.Task : Task.Delay( -1 );

            await Task.WhenAny( delayTask, keyTask, logTask );
            _logUpdateSignal = null;

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
    }

    _logUpdateSignal?.TrySetResult( true );
  }

  private async Task RenderAsync() {
    _layout.UpdateData( "DUMMY" );
    /*int totalHeight = subnetViewPort.GetTotalHeight( _subnets );
    int maxScroll = Math.Max( 0, totalHeight - availableRows );

    // Debug information (remove after fixing)
    _layout.UpdateData(
      $"ScrollOffset: {_scrollOffset}, MaxScroll: {maxScroll}, TotalHeight: {totalHeight}, AvailableRows: {availableRows}" );

    if ( _logEnabled && _logReader != null ) {
      lock ( _logBuilder ) {
        _layout.UpdateLog( _logBuilder.ToString() );
      }
    }

    _scrollOffset = Math.Clamp( _scrollOffset, 0, maxScroll );*/
    SubnetViewPort.Subnets = _subnets;

    //var trees = subnetViewPort.RenderTrees( _subnets );
    if ( _subnets.Count > 0 ) {
      Console.WriteLine( "hi" );
    }

    _layout.UpdateScanTree( SubnetViewPort );

    _layout.UpdateProgress( _progress );
  }

  private void HandleInput( ConsoleKey key ) {
    var action = _keyMap.MapKey( key );

    switch ( action ) {
      case UiAction.Quit:
        _running.Cancel();
        break;
      case UiAction.ScrollUp:
        //_scrollOffset -= SubnetViewPort.ScrollAmount;
        break;
      case UiAction.ScrollDown:
        //_scrollOffset += SubnetViewPort.ScrollAmount;
        break;
      case UiAction.MoveUp:
        //_selectedIndex = Math.Max( 0, _selectedIndex - 1 );
        break;
      case UiAction.MoveDown:
        //_selectedIndex = Math.Min( _subnets.Count - 1, _selectedIndex + 1 );
        break;
      case UiAction.ToggleSubnet:
        //_subnets[_selectedIndex].IsExpanded = !_subnets[_selectedIndex].IsExpanded;
        break;
      case UiAction.RestartScan:
        _subnets.Clear();
        StartScanAsync();
        //_selectedIndex = 0;
        //_scrollOffset = 0;
        break;
      case UiAction.ToggleLog:
        _layout.ShowLogs = !_layout.ShowLogs;
        break;
      case UiAction.None:
      default:
        break;
    }
  }

  private void OnScanResultUpdated( object? sender, NetworkScanResult scanResult ) {
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

    /*if ( _selectedIndex >= _subnets.Count )
      _selectedIndex = Math.Max( 0, _subnets.Count - 1 );*/
  }

  public async ValueTask DisposeAsync() {
    _scanner.ResultUpdated -= OnScanResultUpdated;
    _running.Dispose();
    await _inputWatcher.DisposeAsync();

    if ( _logReader != null ) {
      _logReader.LogUpdated -= OnLogUpdated;
    }
  }
}