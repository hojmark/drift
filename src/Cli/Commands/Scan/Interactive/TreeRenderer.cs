using Drift.Cli.Commands.Scan.Interactive.Models;
using Drift.Domain;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive;

internal static class TreeRenderer {
  private static readonly int StatusWidth = "Offline".Length;

  internal static IEnumerable<Tree> Render(
    List<Subnet> subnets,
    CidrBlock? selected,
    uint viewPortHeight,
    uint scrollOffset
  ) {
    var trees = new List<Tree>();
    int currentRow = 0;
    int renderedRows = 0;

    foreach ( var subnet in subnets ) {
      int treeHeight = subnet.GetHeight();
      bool isSelected = subnet.Cidr == selected;

      // Check if this tree starts within the visible area
      if ( currentRow + treeHeight > scrollOffset && currentRow < scrollOffset + viewPortHeight ) {
        // Calculate how many rows of this tree we can show
        int treeStartRow = (int) Math.Max( 0, scrollOffset - currentRow );
        int remainingRows = (int) ( viewPortHeight - renderedRows );
        int maxDeviceRows = Math.Min( remainingRows - 1, treeHeight - 1 - treeStartRow ); // -1 for header

        if ( treeStartRow == 0 ) {
          // Show the full tree header
          if ( maxDeviceRows >= 0 ) {
            trees.Add( BuildTree( subnet, subnets, isSelected, maxDeviceRows ) );
            renderedRows += Math.Min( treeHeight, remainingRows );
          }
        }
        else if ( treeStartRow < treeHeight && subnet.IsExpanded ) {
          // Show partial tree (skip header, show some devices)
          int devicesToSkip = treeStartRow - 1; // -1 because we're skipping the header
          int devicesToShow = Math.Min( maxDeviceRows + 1, subnet.Devices.Count - devicesToSkip ); // +1 because the header is not shown (makes room for additional device)

          if ( devicesToShow > 0 ) {
            trees.Add( BuildPartialTree( subnet, subnets, devicesToSkip ) );
            renderedRows += devicesToShow;
          }
        }
      }

      currentRow += treeHeight;
    }

    return trees;
  }

  private static Tree BuildPartialTree( Subnet subnet, List<Subnet> subnets, int skipDevices ) {
    // Create a tree with an empty header since we're showing a continuation
    var tree = new Tree( "" ).Guide( TreeGuide.Line );

    var devices = subnet.Devices
      .Skip( skipDevices )
      .ToList();

    foreach ( var device in devices ) {
      var deviceString = RenderDevice( device, subnets );
      tree.AddNode( deviceString );
    }

    return tree;
  }

  private static Tree BuildTree( Subnet subnet, List<Subnet> subnets, bool isSelected, int? maxDeviceCount = null ) {
    var symbol = subnet.IsExpanded ? "▾" : "▸";

    string summary =
      $"[grey]({subnet.Devices.Count} devices: " +
      $"{subnet.Devices.Count( d => d.IsOnline )} online, " +
      $"{subnet.Devices.Count( d => !d.IsOnline )} offline)[/]";

    string header = $"{symbol} {subnet.Cidr}";
    string formattedHeader = isSelected
      ? $"[black on yellow]{header}[/] {summary}"
      : $"[blue]{header}[/] {summary}";

    var tree = new Tree( formattedHeader ).Guide( TreeGuide.Line );

    if ( subnet.IsExpanded ) {
      var devices = subnet.Devices;
      if ( maxDeviceCount is not null )
        devices = devices.Take( maxDeviceCount.Value ).ToList();

      foreach ( var device in devices ) {
        var deviceString = RenderDevice( device, subnets );
        tree.AddNode( deviceString );
      }

      // Optionally show ellipsis if tree was truncated
      if ( maxDeviceCount is not null && maxDeviceCount < subnet.Devices.Count ) {
        tree.AddNode( "[grey]...[/]" );
      }
    }

    return tree;
  }

  private static string RenderDevice( Device device, List<Subnet> subnets ) {
    string statusColor = device.IsOnline ? "green" : "red";
    string statusText = device.IsOnline ? "Online" : "Offline";

    return
      $"[white]{device.Ip.PadRight( subnets.GetIpWidth() )}[/]  " +
      $"[grey]{device.Mac.PadRight( subnets.GetMacWidth() )}[/]  " +
      $"[{statusColor}]{statusText.PadRight( StatusWidth )}[/]";
  }
}