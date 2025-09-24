using System.Collections;
using Drift.Cli.Commands.Scan.Interactive.Models;
using Drift.Domain;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive;

internal class SubnetViewport( uint height ) : IEnumerable<Tree> {
  private static readonly int StatusWidth = "Offline".Length;
  private readonly Lock _subnetLock = new();

  private List<Subnet> _subnets = [];

  public List<Subnet> Subnets {
    set {
      lock ( _subnetLock ) {
        _subnets = value;
        if ( _subnets.FirstOrDefault( s => s.Cidr == Selected ) == null ) {
          Selected = _subnets.FirstOrDefault()?.Cidr;
        }
      }
    }
  }

  private uint MaxScrollOffset => (uint) Math.Max( 0, GetHeight( _subnets ) - height );

  private uint _scrollOffset;

  internal uint ScrollOffset {
    get {
      return _scrollOffset;
    }
    set {
      _scrollOffset = Math.Clamp( value, 0, MaxScrollOffset );
    }
  }

  private CidrBlock? Selected {
    get;
    set;
  }

  public string DebugData {
    get {
      var selectedCidr = _subnets.FirstOrDefault( s => s.Cidr == Selected );
      var selectedIndex = -1;

      if ( selectedCidr != null ) {
        selectedIndex = _subnets.IndexOf( selectedCidr );
      }

      return
        $"ScrollOffset: {ScrollOffset}, MaxScroll: {MaxScrollOffset}, TotalHeight: {GetHeight( _subnets )}, ViewportHeight: {height}, SelectedIndex: {selectedIndex}";
    }
  }

  public void ToggleSelected() {
    lock ( _subnetLock ) {
      var subnet = _subnets.FirstOrDefault( s => s.Cidr == Selected );
      if ( subnet != null ) {
        subnet.IsExpanded = !subnet.IsExpanded;
      }
    }
  }

  public void SelectNext() {
    lock ( _subnetLock ) {
      var subnet = _subnets.FirstOrDefault( s => s.Cidr == Selected );
      if ( subnet == null ) {
        return;
      }

      var index = _subnets.IndexOf( subnet );
      var nextIndex = index + 1;

      if ( nextIndex < _subnets.Count ) {
        Selected = _subnets[nextIndex].Cidr;
      }
    }
  }

  public void SelectPrevious() {
    lock ( _subnetLock ) {
      var subnet = _subnets.FirstOrDefault( s => s.Cidr == Selected );
      if ( subnet == null ) {
        return;
      }

      var index = _subnets.IndexOf( subnet );
      var previousIndex = index - 1;

      if ( previousIndex >= 0 ) {
        Selected = _subnets[previousIndex].Cidr;
      }
    }
  }

  private static IEnumerable<Tree> Render(
    List<Subnet> subnets,
    CidrBlock? selected,
    uint viewPortHeight,
    uint scrollOffset
  ) {
    var trees = new List<Tree>();
    int currentRow = 0;
    int renderedRows = 0;

    foreach ( var subnet in subnets ) {
      int treeHeight = GetHeight( subnet );
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
          int devicesToShow = Math.Min( maxDeviceRows, subnet.Devices.Count - devicesToSkip );

          if ( devicesToShow > 0 ) {
            trees.Add( BuildPartialTree( subnet, subnets, devicesToSkip, devicesToShow ) );
            renderedRows += devicesToShow;
          }
        }
      }

      currentRow += treeHeight;
    }

    return trees;
  }

  private static Tree BuildPartialTree( Subnet subnet, List<Subnet> subnets, int skipDevices, int maxDevices ) {
    // Create a tree with an empty header since we're showing a continuation
    var tree = new Tree( "" ).Guide( TreeGuide.Line );

    var devices = subnet.Devices.Skip( skipDevices ).Take( maxDevices ).ToList();

    foreach ( var device in devices ) {
      string statusColor = device.IsOnline ? "green" : "red";
      string statusText = device.IsOnline ? "Online" : "Offline";

      string line =
        $"[white]{device.Ip.PadRight( GetIpWidth( subnets ) )}[/]  " +
        $"[grey]{device.Mac.PadRight( GetMacWidth( subnets ) )}[/]  " +
        $"[{statusColor}]{statusText.PadRight( StatusWidth )}[/]";

      tree.AddNode( line );
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
        string statusColor = device.IsOnline ? "green" : "red";
        string statusText = device.IsOnline ? "Online" : "Offline";

        string line =
          $"[white]{device.Ip.PadRight( GetIpWidth( subnets ) )}[/]  " +
          $"[grey]{device.Mac.PadRight( GetMacWidth( subnets ) )}[/]  " +
          $"[{statusColor}]{statusText.PadRight( StatusWidth )}[/]";

        tree.AddNode( line );
      }

      // Optionally show ellipsis if tree was truncated
      if ( maxDeviceCount is not null && maxDeviceCount < subnet.Devices.Count ) {
        tree.AddNode( "[grey]...[/]" );
      }
    }

    return tree;
  }


  // TODO below methods should be extension methods
  private static int GetHeight( List<Subnet> subnets ) => subnets.Sum( GetHeight );

  private static int GetHeight( Subnet subnet )
    => 1 + ( subnet.IsExpanded
      ? subnet.Devices.Count
      : 0 );

  private static int GetIpWidth( List<Subnet> subnets ) {
    return subnets.SelectMany( s => s.Devices ).Max( d => d.Ip.Length );
  }

  private static int GetMacWidth( List<Subnet> subnets ) {
    return subnets.SelectMany( s => s.Devices ).Max( d => d.Mac.Length );
  }

  public IEnumerator<Tree> GetEnumerator() {
    lock ( _subnetLock ) {
      var snapshot = _subnets.ToList();
      return Render( snapshot, Selected, height, ScrollOffset ).GetEnumerator();
    }
  }

  IEnumerator IEnumerable.GetEnumerator() {
    return GetEnumerator();
  }
}