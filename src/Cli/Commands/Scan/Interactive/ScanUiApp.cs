using Drift.Cli.Commands.Scan.Interactive.Simulation;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive;

public class ScanUiApp {
  private readonly IScanner _scanner;
  private readonly Layout _layout;
  private int _selectedIndex = 0;
  private int _scrollOffset = 0;
  private bool _running = true;

  //TODO should get IDisposable warning?
  private readonly AsyncKeyInputWatcher _inputWatcher = new();
  private List<UiSubnet> _subnets = new();


  public ScanUiApp( IScanner scanner ) {
    _scanner = scanner;
    _layout = LayoutFactory.Create();
  }

  public async Task RunAsync() {
    _scanner.Start();

    await AnsiConsole
      .Live( _layout )
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
    int availableRows = GetAvailableRows();
    int maxScroll = Math.Max( 0, renderer.GetTotalHeight( subnets ) - availableRows );
    _scrollOffset = Math.Clamp( _scrollOffset, 0, maxScroll );

    var trees = renderer.RenderTrees( _scrollOffset, availableRows, _selectedIndex, subnets );

    _layout["MainPanel"].Update(
      new Panel( new Rows( trees ) ).Expand().Border( BoxBorder.Square ).Padding( 0, 0 )
    );

    _layout["Progress"].Update( LayoutFactory.BuildProgressChart( progress ) );

    _layout["Footer"].Update( BuildFooter( _scrollOffset, maxScroll, _selectedIndex, subnets ) );
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


  public static Markup BuildFooter( int scroll, int maxScroll, int selectedIndex, List<UiSubnet> subnets ) {
    const string keyColor = "blue";
    const string actionColor = "";

    var keyActions = new Dictionary<string, string> {
      { "q", "quit" },
      { "↑/↓" /*"/←/→"*/, "navigate" },
      { "space", "toggle" },
      { "w/s", "scroll" },
      { "h", "help toggle" }
    };

    var footerParts = new List<string>();

    foreach ( var kvp in keyActions ) {
      footerParts.Add( $"[{keyColor}]{kvp.Key}[/] [{actionColor}]{kvp.Value}[/]" );
    }

    footerParts.Add( $"[grey]Scroll: {scroll}/{maxScroll}[/]" );
    footerParts.Add( $"[grey]Selected: {selectedIndex + 1}/{subnets.Count}[/]" );

    return new Markup( string.Join( "   ", footerParts ) );
  }

  private int GetAvailableRows()
    => AnsiConsole.Console.Profile.Height - 1 - 1 - 1 - 2; // header + footer + progress + padding
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