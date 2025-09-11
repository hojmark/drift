using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive;

public class TreeRenderer {
  public const int ScrollAmount = 3;

  private readonly bool[] _expanded;
  private readonly int _statusWidth = "Offline".Length;

  public TreeRenderer( bool[] expanded ) {
    _expanded = expanded;
  }

  public int GetTotalHeight( List<Subnet> subnets )
    => subnets.Select( ( _, i ) => GetTreeHeight( i, subnets ) ).Sum();

  public IEnumerable<Tree> RenderTrees( int scrollOffset, int maxRows, int selectedIndex, List<Subnet> subnets ) {
    var trees = new List<Tree>();
    int usedRows = 0, skippedRows = 0, startIndex = 0;

    // Skip trees that are completely above scroll offset
    for ( ; startIndex < subnets.Count; startIndex++ ) {
      int height = GetTreeHeight( startIndex, subnets );
      if ( skippedRows + height > scrollOffset )
        break;

      skippedRows += height;
    }

    for ( int i = startIndex; i < subnets.Count && usedRows < maxRows; i++ ) {
      int totalHeight = GetTreeHeight( i, subnets );
      int remaining = maxRows - usedRows;
      bool isSelected = i == selectedIndex;

      if ( totalHeight <= remaining ) {
        trees.Add( BuildTree( i, isSelected, subnets ) );
        usedRows += totalHeight;
      }
      else {
        // Always render header (1 row), then as many children as possible
        int rowsForChildren = remaining - 1;

        trees.Add( BuildTree( i, isSelected, subnets, rowsForChildren > 0 ? rowsForChildren : 0 ) );
        usedRows = maxRows;
      }
    }

    return trees;
  }


  private Tree BuildTree( int index, bool isSelected, List<Subnet> subnets, int? maxDeviceCount = null ) {
    var subnet = subnets[index];
    var symbol = _expanded[index] ? "▾" : "▸";

    string summary = !_expanded[index]
      ? $" [grey]({subnet.Devices.Count} devices: " +
        $"{subnet.Devices.Count( d => d.IsOnline )} online, " +
        $"{subnet.Devices.Count( d => !d.IsOnline )} offline)[/]"
      : "";

    string header = $"{symbol} {subnet.Address}{summary}";
    string formattedHeader = isSelected
      ? $"[black on yellow]{header}[/]"
      : $"[teal]{header}[/]";

    var tree = new Tree( formattedHeader ).Guide( TreeGuide.Line );

    if ( _expanded[index] ) {
      var devices = subnet.Devices;
      if ( maxDeviceCount is not null )
        devices = devices.Take( maxDeviceCount.Value ).ToList();

      foreach ( var device in devices ) {
        string statusColor = device.IsOnline ? "green" : "red";
        string statusText = device.IsOnline ? "Online" : "Offline";

        string line =
          $"[white]{device.IP.PadRight( GetIpWidth( subnets ) )}[/]  " +
          $"[grey]{device.MAC.PadRight( GetMacWidth( subnets ) )}[/]  " +
          $"[{statusColor}]{statusText.PadRight( _statusWidth )}[/]";

        tree.AddNode( line );
      }

      // Optionally show ellipsis if tree was truncated
      if ( maxDeviceCount is not null && maxDeviceCount < subnet.Devices.Count ) {
        tree.AddNode( "[grey]...[/]" );
      }
    }

    return tree;
  }


  private int GetTreeHeight( int index, List<Subnet> subnets )
    => _expanded[index] ? 1 + subnets[index].Devices.Count : 1;

  private int GetIpWidth( List<Subnet> subnets ) {
    return subnets.SelectMany( s => s.Devices ).Max( d => d.IP.Length );
  }

  private int GetMacWidth( List<Subnet> subnets ) {
    return subnets.SelectMany( s => s.Devices ).Max( d => d.MAC.Length );
  }
}