using Drift.Core.Scan.Simulation.Models;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Extensions;
using Drift.Domain.Scan;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive;

internal class InteractiveScanUi {
  private NetworkScanOptions _scanRequest;
  private readonly INetworkScanner _scanner;
  private readonly ScanLayout _layout2;
  private int _selectedIndex;
  private int _scrollOffset;
  private bool _running = true;

  //TODO should get IDisposable warning?
  private readonly AsyncKeyInputWatcher _inputWatcher = new();
  private readonly List<UiSubnet> _subnets = [];
  private Percentage _progress;


  public InteractiveScanUi( INetworkScanner scanner ) {
    _scanner = scanner;
    _layout2 = new ScanLayout();

    // Subscribe to events instead of polling
    //_scanner.SubnetsUpdated += OnSubnetsUpdated;
    _scanner.ResultUpdated += OnScanResultUpdated;
  }

  private Task<NetworkScanResult> StartScanAsync() {
    return _scanner.ScanAsync( _scanRequest );
  }

  public async Task RunAsync( NetworkScanOptions scanRequest ) {
    _scanRequest = scanRequest;
    var scanTask = StartScanAsync();

    await AnsiConsole
      .Live( _layout2.Renderable )
      .AutoClear( true )
      .StartAsync( async ctx => {
          while ( _running ) {
            Task[] renderCriteria = [_inputWatcher.WaitForNextKeyAsync(), Task.Delay( 250 )];

            await Task.WhenAny( renderCriteria );

            var key = _inputWatcher.ConsumeKey();
            if ( key != null ) {
              HandleInput( key.Value, _subnets );
            }

            Render();

            ctx.Refresh();
          }
        }
      );
  }

  private void Render() {
    var renderer = new TreeRenderer();
    int availableRows = _layout2.GetAvailableRows();
    int totalHeight = renderer.GetTotalHeight( _subnets );
    int maxScroll = Math.Max( 0, totalHeight - availableRows );

    // Debug information (remove after fixing)
    _layout2.UpdateData(
      $"ScrollOffset: {_scrollOffset}, MaxScroll: {maxScroll}, TotalHeight: {totalHeight}, AvailableRows: {availableRows}" );

    _scrollOffset = Math.Clamp( _scrollOffset, 0, maxScroll );

    var trees = renderer.RenderTrees( _scrollOffset, availableRows, _selectedIndex, _subnets );

    _layout2.UpdateScanTree( trees );

    _layout2.UpdateProgress( _progress );
  }

  private void HandleInput( ConsoleKey key, List<UiSubnet> subnets ) {
    var action = InputMapper.MapKey( key );

    switch ( action ) {
      case InputAction.Quit:
        _running = false;
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
        _selectedIndex = Math.Min( subnets.Count - 1, _selectedIndex + 1 );
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
        _layout2.ShowLogs = !_layout2.ShowLogs;
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
}

internal class UiSubnet {
  public Subnet Subnet {
    get;
  }

  public bool IsExpanded {
    get;
    set;
  }

  public UiSubnet( Subnet subnet, bool isExpanded = true ) {
    Subnet = subnet;
    IsExpanded = isExpanded;
  }
}