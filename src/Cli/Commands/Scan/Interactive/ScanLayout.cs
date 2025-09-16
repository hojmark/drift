namespace Drift.Cli.Commands.Scan.Interactive;

using Spectre.Console;
using Spectre.Console.Rendering;

public class ScanLayout {
  private readonly Layout _layout;

  private bool _showLogs = true;
  private readonly Layout _scanTree;
  private readonly Layout _log;

  public bool ShowLogs {
    get => _showLogs;
    set {
      if ( _showLogs == value )
        return;

      _showLogs = value;
      UpdateMainPanel( value );
    }
  }

  public ScanLayout() {
    _layout = new Layout( "Root" )
      .SplitRows(
        new Layout( "Header" ) { Size = 1 },
        new Layout( "MainPanel" ),
        new Layout( "Data" ) { Size = 1 },
        new Layout( "Progress" ) { Size = 1 },
        new Layout( "Footer" ) { Size = 1 }
      );

    _scanTree = new Layout( "ScanTree" );
    _log = new Layout( "Log" );

    _layout["Header"].Update( BuildHeader() );
    _layout["Progress"].Update( BuildProgressBar( 0 ) );
    _layout["Footer"].Update( BuildFooter() );

    UpdateMainPanel( _showLogs );
  }

  private void UpdateMainPanel( bool showLogs ) {
    var mainPanelChildren = new List<Layout> { _scanTree };

    if ( showLogs ) {
      mainPanelChildren.Add( _log );
    }

    _layout["MainPanel"].SplitColumns(mainPanelChildren.ToArray());

  }


  public IRenderable Renderable => _layout;

  public void UpdateScanTree( IEnumerable<Tree> content ) {
    _scanTree.Update(
      new Panel( new Rows( content ) ).Expand().Border( BoxBorder.Square ).Padding( 0, 0 )
    );
  }

  public void UpdateProgress( uint progress ) {
    _layout["Progress"].Update( BuildProgressBar( progress ) );
  }

  public void UpdateData( string text ) {
    _layout["Data"].Update( new Text( text ) );
  }

  internal int GetAvailableRows()
    => AnsiConsole.Console.Profile.Height - 1 - 1 - 1 - 1 - 2; // header + data + footer + progress + padding

  private static Markup BuildHeader() {
    return new Markup( "Using [grey]/home/hojmark/[/][yellow bold]fh47[/][grey].spec.yaml[/]  [green]✔[/]" );
  }

  private static BreakdownChart BuildProgressBar( uint progress ) {
    return new BreakdownChart()
      .HideTags()
      .Width( AnsiConsole.Console.Profile.Width )
      .AddItem( "Good", progress, Color.Green )
      //.AddItem("Unknown", 5, Color.Yellow)
      //.AddItem("Bad", 2, Color.Red)
      .AddItem( "Remaining", 100 - progress, Color.Grey );
  }

  private static Markup BuildFooter(  /*int scroll, int maxScroll, int selectedIndex, List<UiSubnet> subnets */ ) {
    const string keyColor = "blue";
    const string actionColor = "";

    var keyActions = new Dictionary<string, string> {
      { "q", "quit" },
      { "r", "restart" },
      { "↑/↓" /*"/←/→"*/, "navigate" },
      { "space", "expansion" },
      { "w/s", "scroll" },
      { "l", "log" },
      { "h", "help" }
    };

    var footerParts = new List<string>();

    foreach ( var kvp in keyActions ) {
      footerParts.Add( $"[{keyColor}]{kvp.Key}[/] [{actionColor}]{kvp.Value}[/]" );
    }

    //footerParts.Add( $"[grey]Scroll: {scroll}/{maxScroll}[/]" );
    //footerParts.Add( $"[grey]Selected: {selectedIndex + 1}/{subnets.Count}[/]" );

    return new Markup( string.Join( "   ", footerParts ) );
  }
}