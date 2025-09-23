using Drift.Cli.Commands.Scan.Interactive.Models;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive;

internal class TreeRenderer {
  public const int ScrollAmount = 3;
  private readonly int _statusWidth = "Offline".Length;

  public int GetTotalHeight( List<Subnet> subnets )
    => subnets.Select( ( _, i ) => GetTreeHeight( i, subnets ) ).Sum();

  public IEnumerable<Tree> RenderTrees( int scrollOffset, int maxRows, int selectedIndex, List<Subnet> subnets ) {
    var trees = new List<Tree>();
    int currentRow = 0;
    int renderedRows = 0;

    for ( int i = 0; i < subnets.Count && renderedRows < maxRows; i++ ) {
      int treeHeight = GetTreeHeight( i, subnets );
      bool isSelected = i == selectedIndex;

      // Check if this tree starts within the visible area
      if ( currentRow + treeHeight > scrollOffset && currentRow < scrollOffset + maxRows ) {
        // Calculate how many rows of this tree we can show
        int treeStartRow = Math.Max( 0, scrollOffset - currentRow );
        int remainingRows = maxRows - renderedRows;
        int maxDeviceRows = Math.Min( remainingRows - 1, treeHeight - 1 - treeStartRow ); // -1 for header

        if ( treeStartRow == 0 ) {
          // Show the full tree header
          if ( maxDeviceRows >= 0 ) {
            trees.Add( BuildTree( i, isSelected, subnets, maxDeviceRows ) );
            renderedRows += Math.Min( treeHeight, remainingRows );
          }
        }
        else if ( treeStartRow < treeHeight && subnets[i].IsExpanded ) {
          // Show partial tree (skip header, show some devices)
          int devicesToSkip = treeStartRow - 1; // -1 because we're skipping the header
          int devicesToShow = Math.Min( maxDeviceRows, subnets[i].Devices.Count - devicesToSkip );

          if ( devicesToShow > 0 ) {
            trees.Add( BuildPartialTree( i, isSelected, subnets, devicesToSkip, devicesToShow ) );
            renderedRows += devicesToShow;
          }
        }
      }

      currentRow += treeHeight;
    }

    return trees;
  }

  private Tree BuildPartialTree( int index, bool isSelected, List<Subnet> subnets, int skipDevices, int maxDevices ) {
    var subnet = subnets[index];

    // Create a tree with an empty header since we're showing a continuation
    var tree = new Tree( "" ).Guide( TreeGuide.Line );

    var devices = subnet.Devices.Skip( skipDevices ).Take( maxDevices ).ToList();

    foreach ( var device in devices ) {
      string statusColor = device.IsOnline ? "green" : "red";
      string statusText = device.IsOnline ? "Online" : "Offline";

      string line =
        $"[white]{device.Ip.PadRight( GetIpWidth( subnets ) )}[/]  " +
        $"[grey]{device.Mac.PadRight( GetMacWidth( subnets ) )}[/]  " +
        $"[{statusColor}]{statusText.PadRight( _statusWidth )}[/]";

      tree.AddNode( line );
    }

    return tree;
  }


  private Tree BuildTree( int index, bool isSelected, List<Subnet> subnets, int? maxDeviceCount = null ) {
    var subnet = subnets[index];
    var symbol = subnet.IsExpanded ? "▾" : "▸";

    string summary =
      $"[grey]({subnet.Devices.Count} devices: " +
      $"{subnet.Devices.Count( d => d.IsOnline )} online, " +
      $"{subnet.Devices.Count( d => !d.IsOnline )} offline)[/]";

    string header = $"{symbol} {subnet.Address}";
    string formattedHeader = isSelected
      ? $"[black on yellow]{header}[/] {summary}"
      : $"[blue]{header}[/] {summary}";

    var tree = new Tree( formattedHeader ).Guide( TreeGuide.Line );

    if ( subnet.IsExpanded ) {
      var devices = subnet.Devices;
      if ( maxDeviceCount is not null )
        devices = devices.Take( maxDeviceCount.Value ).ToList();

      foreach ( var device in devices ) {
        string statusColor = device.IsOnline ? "green" : "red";
        string statusText = device.IsOnline ? "Online" : "Offline";

        string line =
          $"[white]{device.Ip.PadRight( GetIpWidth( subnets ) )}[/]  " +
          $"[grey]{device.Mac.PadRight( GetMacWidth( subnets ) )}[/]  " +
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
    => subnets[index].IsExpanded ? 1 + subnets[index].Devices.Count : 1;

  private int GetIpWidth( List<Subnet> subnets ) {
    return subnets.SelectMany( s => s.Devices ).Max( d => d.Ip.Length );
  }

  private int GetMacWidth( List<Subnet> subnets ) {
    return subnets.SelectMany( s => s.Devices ).Max( d => d.Mac.Length );
  }
}