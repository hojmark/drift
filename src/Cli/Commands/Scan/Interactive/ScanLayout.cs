using Drift.Domain;

namespace Drift.Cli.Commands.Scan.Interactive;

using Spectre.Console;
using Spectre.Console.Rendering;

internal class ScanLayout {
  private readonly Layout _layout;

  public bool ShowLogs {
    get => _layout["Log"].IsVisible;
    set {
      _layout["Log"].IsVisible = value;
      _layout["MainPanel"].Update( _layout["MainPanel"] );
    }
  }

  public ScanLayout() {
    _layout = new Layout( "Root" )
      .SplitRows(
        new Layout( "Header" ) { Size = 1 },
        new Layout( "MainPanel" ).SplitColumns(
          new Layout( "ScanTree" ),
          new Layout( "Log" ) { IsVisible = false }
        ),
        new Layout( "Data" ) { Size = 1 },
        new Layout( "Progress" ) { Size = 1 },
        new Layout( "Footer" ) { Size = 1 }
      );

    _layout["Header"].Update( BuildHeader() );
    UpdateProgress( Percentage.Zero );
    _layout["Footer"].Update( BuildFooter() );
  }

  public IRenderable Renderable => _layout;

  public void UpdateScanTree( IEnumerable<Tree> content ) {
    _layout["ScanTree"].Update(
      new Panel( new Rows( content ) ).Expand().Border( BoxBorder.Square ).Padding( 0, 0 )
    );
  }

  public void UpdateProgress( Percentage progress ) {
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

  private static Layout BuildProgressBar( Percentage progress ) {
    var progressValue = $" {progress}";

    return new Layout( "ProgressComponents" ).SplitColumns(
      new Layout( new Text( progress.Value switch {
        0 => "Idle",
        100 => "Completed",
        _ => "Scanning..."
      } ) ) { Size = "Scanning...".Length + 1 },
      new Layout(
        new BreakdownChart()
          .HideTags()
          //.Width( AnsiConsole.Console.Profile.Width )
          .AddItem( "Good", progress, Color.Green )
          //.AddItem("Unknown", 5, Color.Yellow)
          //.AddItem("Bad", 2, Color.Red)
          .AddItem( "Remaining", Percentage.Hundred.Value - progress, Color.Grey ).Expand()
      ),
      new Layout( new Text( $" {progressValue}" ) ) { Size = progressValue.Length + 1 }
    );
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
      // TODO { "l", "log" },
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