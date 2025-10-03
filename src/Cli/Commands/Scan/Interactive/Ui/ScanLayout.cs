using Drift.Domain;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Drift.Cli.Commands.Scan.Interactive.Ui;

internal class ScanLayout( NetworkId? networkId ) {
  private readonly Layout _layout = new Layout( "Root" )
    .SplitRows(
      new Layout( "Header", BuildHeader( networkId ) ) { Size = 1 },
      new Layout( "MainPanel" ).SplitColumns(
        new Layout( "ScanTree" ),
        new Layout( "Log" ) { IsVisible = false }
      ),
      new Layout( "Debug" ) { Size = 1, IsVisible = false },
      new Layout( "Progress", BuildProgressBar( Percentage.Zero ) ) { Size = 1 },
      new Layout( "Footer", BuildFooter() ) { Size = 1 }
    );

  public IRenderable Renderable => _layout;

  public bool ShowLogs {
    get => _layout["Log"].IsVisible;
    set {
      _layout["Log"].IsVisible = value;
      _layout["MainPanel"].Update( _layout["MainPanel"] );
    }
  }

  public bool ShowDebug {
    get => _layout["Debug"].IsVisible;
    set {
      _layout["Debug"].IsVisible = value;
      // _layout["Debug"].Update( _layout["Debug"] );
      // Re-render mainpanel, as there is now more room available
      _layout["MainPanel"].Update( _layout["MainPanel"] );
    }
  }

  public uint AvailableRows {
    get {
      var rows = AnsiConsole.Console.Profile.Height -
                 // header + data (optional) + footer + progress + padding
                 1 - ( _layout["Debug"].IsVisible ? 1 : 0 ) - 1 - 1 - 2;
      return (uint) Math.Max( 0, rows );
    }
  }

  public void SetScanTree( IEnumerable<Tree> content ) {
    _layout["ScanTree"].Update(
      new Panel( new Rows( content ) ).Expand().Border( BoxBorder.Square ).Padding( 0, 0 )
    );
  }

  public void SetProgress( Percentage progress ) {
    _layout["Progress"].Update( BuildProgressBar( progress ) );
  }

  public void SetDebug( string text ) {
    _layout["Debug"].Update( new Text( text ) );
  }

  public void SetLog( IRenderable text ) {
    _layout["Log"].Update( new Panel( text ).Expand().Border( BoxBorder.Square ).Padding( 0, 0 ) );
  }

  private static Markup BuildHeader( NetworkId? id ) {
    // TODO update with actual path
    return id == null
      ? new Markup( "[yellow bold]unknown network[/]" )
      : new Markup( $"[bold]{( InteractiveUi.FakeData ? "main-site" : id.Value )}[/] [green]✔[/]" ) {
        Justification = Justify.Left
      };
  }

  private static Layout BuildProgressBar( Percentage progress ) {
    var progressValue = $" {progress}";

    return new Layout( "ProgressComponents" ).SplitColumns(
      new Layout(
        new Text(
          progress.Value switch {
            0 => "Idle",
            100 => "Completed",
            _ => "Scanning..."
          }
        )
      ) { Size = "Scanning...".Length + 1 },
      new Layout(
        new BreakdownChart()
          .HideTags()
          // .Width( AnsiConsole.Console.Profile.Width )
          .AddItem( "Completed", progress, Color.White )
          // .AddItem( "Good", progress, Color.Green )
          // .AddItem("Unknown", 5, Color.Yellow)
          // .AddItem("Bad", 2, Color.Red)
          .AddItem( "Remaining", Percentage.Hundred.Value - progress, Color.Grey ).Expand()
      ),
      new Layout( new Text( $" {progressValue}" ) ) { Size = progressValue.Length + 1 }
    );
  }

  private static Markup BuildFooter( /*int scroll, int maxScroll, int selectedIndex, List<UiSubnet> subnets */ ) {
    const string keyColor = "bold";
    const string actionColor = "";

    var keyActions = new Dictionary<string, string> {
      { "q", "quit" },
      { "r", "restart" },
      { "↑/↓" /*"/←/→"*/, "navigate" },
      { "space", "toggle" },
      { "w/s", "scroll" },
      { "l", "log" },
      // { "v", "view" },
      // { "h", "help" }
    };

    var footerParts = new List<string>();

    foreach ( var kvp in keyActions ) {
      footerParts.Add( $"[{keyColor}]{kvp.Key}[/][{actionColor}] {kvp.Value}[/]" );
    }

    // footerParts.Add( $"[grey]Scroll: {scroll}/{maxScroll}[/]" );
    // footerParts.Add( $"[grey]Selected: {selectedIndex + 1}/{subnets.Count}[/]" );

    return new Markup( string.Join( $"[{actionColor}]   [/]", footerParts ) );
  }
}