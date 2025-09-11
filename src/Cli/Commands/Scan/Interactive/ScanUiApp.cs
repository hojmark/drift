using Drift.Cli.Commands.Scan.Interactive.Simulation;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive;

public class ScanUiApp {
  private readonly IScanner _scanner;
  private readonly bool[] _expanded;
  private readonly Layout _layout;
  private int _selectedIndex = 0;
  private int _scrollOffset = 0;
  private bool _running = true;

  private readonly TreeRenderer _renderer;
  private readonly InputHandler _input;

  private readonly AutoResetEvent _renderTrigger = new(false);
  private ConsoleKey? _latestKey;

  public ScanUiApp( IScanner scanner ) {
    _scanner = scanner;

    _expanded = Enumerable.Repeat( true, scanner.GetCurrentSubnets().Count ).ToArray();
    _layout = LayoutFactory.Create();
    _renderer = new TreeRenderer( _expanded );
    _input = new InputHandler( scanner.GetCurrentSubnets().Count );
  }

  public void Run() {
    _scanner.Start();
    
    //Key thread
    Task.Run( () => {
      while ( _running ) {
        if ( Console.KeyAvailable ) {
          var key = Console.ReadKey( true ).Key;
          _latestKey = key;
          _renderTrigger.Set(); // Wake render loop
        }

        Thread.Sleep( 10 );
      }
    } );

    AnsiConsole.Live( _layout ).Start( ctx => {


      while ( _running ) {
        // Wait for signal OR timeout
        _renderTrigger.WaitOne(TimeSpan.FromMilliseconds(250));
        
        var subnets = _scanner.GetCurrentSubnets().ToList();
        
        // Check and handle input if there was any
        if (_latestKey is ConsoleKey key)
        {
          HandleInput(key, subnets);
          _latestKey = null;
        }
        
        Render( ctx, subnets, _scanner.Progress );
      }
    } );

    AnsiConsole.Clear();
    //AnsiConsole.MarkupLine( "[bold green]Exited.[/]" );
  }

  private void Render( LiveDisplayContext ctx, List<Subnet> subnets, uint progress ) {
    int availableRows = GetAvailableRows();
    int maxScroll = Math.Max( 0, _renderer.GetTotalHeight( subnets ) - availableRows );
    _scrollOffset = Math.Clamp( _scrollOffset, 0, maxScroll );

    var trees = _renderer.RenderTrees( _scrollOffset, availableRows, _selectedIndex, subnets );

    _layout["MainPanel"].Update(
      new Panel( new Rows( trees ) ).Expand().Border( BoxBorder.Square ).Padding( 0, 0 )
    );

    _layout["Progress"].Update( LayoutFactory.BuildProgressChart( progress ) );

    _layout["Footer"].Update( BuildFooter( _scrollOffset, maxScroll, _selectedIndex, subnets ) );

    ctx.Refresh();
  }

  private void HandleInput(ConsoleKey key, List<Subnet> subnets ) {

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
        _expanded[_selectedIndex] = true;
        break;
      case InputAction.Collapse:
        _expanded[_selectedIndex] = false;
        break;
      case InputAction.ToggleSelected:
        _expanded[_selectedIndex] = !_expanded[_selectedIndex];
        break;
      case InputAction.ToggleByIndex:
        int idx = _input.GetNumericIndex( key );
        if ( idx < subnets.Count ) _expanded[idx] = !_expanded[idx];
        break;
    }
  }

  public static Markup BuildFooter( int scroll, int maxScroll, int selectedIndex, List<Subnet> subnets )
    => new(
      $"[green]q[/] quit   [green]↑/↓/←/→[/] navigate   [green]space[/] toggle   " +
      $"[green]w/s[/] scroll   [grey]Scroll: {scroll}/{maxScroll}[/]  " +
      $"[grey]Selected: {selectedIndex + 1}/{subnets.Count}[/]"
    );

  private int GetAvailableRows()
    => AnsiConsole.Console.Profile.Height - 1 - 1 - 1 - 2; // header + footer + progress + padding
}