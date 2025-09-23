using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Scan.Interactive.Models;
using Drift.Cli.Output.Abstractions;
using Drift.Cli.Output.Logging;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Extensions;
using Drift.Domain.Scan;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive;

internal class InteractiveScanUi : IAsyncDisposable {
  private readonly IOutputManager _outputManager;
  private NetworkScanOptions _scanRequest;
  private readonly INetworkScanner _scanner;
  private readonly ScanLayout _layout;
  private int _selectedIndex;
  private int _scrollOffset;
  private readonly CancellationTokenSource _running = new();
  private const int RenderRefreshIntervalMs = 250;


  //TODO should get IDisposable warning?
  private readonly AsyncKeyInputWatcher _inputWatcher = new();
  private readonly List<UiSubnet> _subnets = [];
  private Percentage _progress;

  public InteractiveScanUi( IOutputManager outputManager, INetworkScanner scanner ) {
    _scanner = scanner;
    _outputManager = outputManager;
    _layout = new ScanLayout();

    // Subscribe to events instead of polling
    //_scanner.SubnetsUpdated += OnSubnetsUpdated;
    _scanner.ResultUpdated += OnScanResultUpdated;
  }

  private Task<NetworkScanResult> StartScanAsync() {
    return _scanner.ScanAsync( _scanRequest, _outputManager.GetLogger() );
  }

  private readonly bool _logEnabled = false;
  private string _log = string.Empty;

  public async Task<int> RunAsync( NetworkScanOptions scanRequest ) {
    _scanRequest = scanRequest;
    var scanTask = StartScanAsync();

    if ( _logEnabled ) {
      _ = Task.Run( ReadLogAsync );
    }

    await AnsiConsole
      .Live( _layout.Renderable )
      .AutoClear( true )
      .StartAsync( async ctx => {
          while ( !_running.IsCancellationRequested ) {
            Task[] renderCriteria = [_inputWatcher.WaitForNextKeyAsync(), Task.Delay( RenderRefreshIntervalMs )];

            await Task.WhenAny( renderCriteria );

            var key = _inputWatcher.ConsumeKey();
            if ( key != null ) {
              HandleInput( key.Value );
            }

            await RenderAsync();

            ctx.Refresh();
          }
        }
      );

    return ExitCodes.Success;
  }

  private async Task ReadLogAsync() {
    while ( true ) {
      var line = await _outputManager.GetReader().ReadLineAsync();
      if ( line != null ) {
        _log += string.IsNullOrEmpty( _log ) ? line : "\n" + line;
      }
      else {
        await Task.Delay( 50 );
      }
    }
  }

  private async Task RenderAsync() {
    var renderer = new TreeRenderer();
    int availableRows = _layout.GetAvailableRows();
    int totalHeight = renderer.GetTotalHeight( _subnets );
    int maxScroll = Math.Max( 0, totalHeight - availableRows );

    // Debug information (remove after fixing)
    _layout.UpdateData(
      $"ScrollOffset: {_scrollOffset}, MaxScroll: {maxScroll}, TotalHeight: {totalHeight}, AvailableRows: {availableRows}" );

    if ( _logEnabled ) {
      _layout.UpdateLog( _log );
    }

    _scrollOffset = Math.Clamp( _scrollOffset, 0, maxScroll );

    var trees = renderer.RenderTrees( _scrollOffset, availableRows, _selectedIndex, _subnets );

    _layout.UpdateScanTree( trees );

    _layout.UpdateProgress( _progress );
  }

  private void HandleInput( ConsoleKey key ) {
    var action = InputMapper.MapKey( key );

    switch ( action ) {
      case InputAction.Quit:
        _running.Cancel();
        break;
      case InputAction.ScrollUp:
        _scrollOffset -= TreeRenderer.ScrollAmount;
        break;
      case InputAction.ScrollDown:
        _scrollOffset += TreeRenderer.ScrollAmount;
        break;
      case InputAction.MoveUp:
        _selectedIndex = Math.Max( 0, _selectedIndex - 1 );
        break;
      case InputAction.MoveDown:
        _selectedIndex = Math.Min( _subnets.Count - 1, _selectedIndex + 1 );
        break;
      case InputAction.Expand:
        _subnets[_selectedIndex].IsExpanded = true;
        break;
      case InputAction.Collapse:
        _subnets[_selectedIndex].IsExpanded = false;
        break;
      case InputAction.ToggleSelected:
        _subnets[_selectedIndex].IsExpanded = !_subnets[_selectedIndex].IsExpanded;
        break;
      case InputAction.RestartScan:
        _subnets.Clear();
        StartScanAsync();
        _selectedIndex = 0;
        _scrollOffset = 0;
        break;
      case InputAction.ToggleLog:
        _layout.ShowLogs = !_layout.ShowLogs;
        break;
    }
  }

  private void OnScanResultUpdated( object? sender, NetworkScanResult scanResult ) {
    _progress = scanResult.Progress;

    List<Subnet> currentSubnets = scanResult.Subnets
      .Select( kvp => new Subnet {
        Address = kvp.CidrBlock.ToString(),
        Devices = kvp.DiscoveredDevices.Select( dd =>
          new Device {
            Ip = dd.Get( AddressType.IpV4 ) ?? "n/a", Mac = dd.Get( AddressType.Mac ) ?? "n/a", IsOnline = true
          } ).ToList()
      } ).ToList();

    // Same logic as before, but triggered by events
    var existingSubnetsMap = _subnets.ToDictionary( ui => ui.Subnet.Address, ui => ui );
    var updatedUiSubnets = new List<UiSubnet>();

    foreach ( var subnet in currentSubnets ) {
      if ( existingSubnetsMap.TryGetValue( subnet.Address, out var existingUiSubnet ) ) {
        updatedUiSubnets.Add( new UiSubnet( subnet, existingUiSubnet.IsExpanded ) );
      }
      else {
        updatedUiSubnets.Add( new UiSubnet( subnet, isExpanded: true ) );
      }
    }

    _subnets.Clear();
    _subnets.AddRange( updatedUiSubnets );

    if ( _selectedIndex >= _subnets.Count )
      _selectedIndex = Math.Max( 0, _subnets.Count - 1 );
  }

  // TODO keymaps: default, vim, emacs, etc.
  public async ValueTask DisposeAsync() {
    _scanner.ResultUpdated -= OnScanResultUpdated;
    _running.Dispose();
    await _inputWatcher.DisposeAsync();
  }
}