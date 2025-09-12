using Drift.Cli.Commands.Scan.Interactive.Simulation;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive;

public class ScanUiApp {
  private readonly IScanner _scanner;
  private readonly ScanLayout _layout2;
  private int _selectedIndex = 0;
  private int _scrollOffset = 0;
  private bool _running = true;

  //TODO should get IDisposable warning?
  private readonly AsyncKeyInputWatcher _inputWatcher = new();
  private readonly List<UiSubnet> _subnets = [];


  public ScanUiApp( IScanner scanner ) {
    _scanner = scanner;
    _layout2 = new ScanLayout();
  }

  public async Task RunAsync() {
    _scanner.Start();

    await AnsiConsole
      .Live( _layout2.Renderable )
      .AutoClear( true )
      .StartAsync( async ctx => {
        while ( _running ) {
          await Task.WhenAny( _inputWatcher.WaitForNextKeyAsync(), Task.Delay( 250 ) );

          var key = _inputWatcher.ConsumeKey();
          if ( key is { } pressed )
            HandleInput( pressed, _subnets );

          UpdateSubnetsFromScanner();
          Render( _subnets, _scanner.Progress );

          ctx.Refresh();
        }
      } );
  }

  private void Render( List<UiSubnet> subnets, uint progress ) {
    var renderer = new TreeRenderer();
    int availableRows = _layout2.GetAvailableRows();
    int maxScroll = Math.Max( 0, renderer.GetTotalHeight( subnets ) - availableRows );
    _scrollOffset = Math.Clamp( _scrollOffset, 0, maxScroll );

    var trees = renderer.RenderTrees( _scrollOffset, availableRows, _selectedIndex, subnets );

    _layout2.UpdateMainPanel( trees );

    _layout2.UpdateProgress( progress );
  }

  private void HandleInput( ConsoleKey key, List<UiSubnet> subnets ) {
    var action = InputHandler.MapKey( key );

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
        _scanner.Start();
        _selectedIndex = 0;
        _scrollOffset = 0;
        break;
    }
  }

  private void UpdateSubnetsFromScanner() {
    var currentSubnets = _scanner.GetCurrentSubnets().ToList();

    // Create a dictionary to track existing subnets by their address for fast lookup
    var existingSubnetsMap = _subnets.ToDictionary( ui => ui.Subnet.Address, ui => ui );

    var updatedUiSubnets = new List<UiSubnet>();

    // Process current subnets from scanner
    foreach ( var subnet in currentSubnets ) {
      if ( existingSubnetsMap.TryGetValue( subnet.Address, out var existingUiSubnet ) ) {
        // Update existing subnet with fresh data while preserving UI state
        updatedUiSubnets.Add( new UiSubnet( subnet, existingUiSubnet.IsExpanded ) );
      }
      else {
        // New subnet - add to the end with default expanded state
        updatedUiSubnets.Add( new UiSubnet( subnet, isExpanded: true ) );
      }
    }

    _subnets.Clear();
    _subnets.AddRange( updatedUiSubnets );

    // Ensure selected index is still valid
    if ( _selectedIndex >= _subnets.Count )
      _selectedIndex = Math.Max( 0, _subnets.Count - 1 );
  }

  // TODO keymaps: default, vim, emacs, etc.
}

public class UiSubnet {
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