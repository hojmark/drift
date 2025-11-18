using System.Collections;
using Drift.Cli.Commands.Scan.Models;
using Drift.Domain;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive.Ui;

internal class SubnetView( Func<uint> height ) : IEnumerable<Tree> {
  private readonly Lock _subnetLock = new();
  private uint _scrollOffset;

  public List<Subnet> Subnets {
    private get;
    set {
      lock ( _subnetLock ) {
        field = value;
        if ( field.FirstOrDefault( s => s.Cidr == Selected ) == null ) {
          Selected = field.FirstOrDefault()?.Cidr;
        }

        if ( MaxScrollOffset == 0 ) {
          ScrollOffset = 0;
        }
      }
    }
  } = [];

  public string DebugData {
    get {
      var selectedCidr = Subnets.FirstOrDefault( s => s.Cidr == Selected );
      var selectedIndex = -1;

      if ( selectedCidr != null ) {
        selectedIndex = Subnets.IndexOf( selectedCidr );
      }

      return
        $"{nameof(ScrollOffset)}: {ScrollOffset}, {nameof(MaxScrollOffset)}: {MaxScrollOffset}, TotalHeight: {Subnets.GetHeight()}, ViewportHeight: {height()}, SelectedIndex: {selectedIndex}";
    }
  }

  // Note: allow setting negative values; values outside the range will be clamped
  internal int ScrollOffset {
    get {
      return (int) _scrollOffset;
    }

    set {
      _scrollOffset = (uint) Math.Clamp( value, 0, MaxScrollOffset );
    }
  }

  private uint MaxScrollOffset => (uint) Math.Max( 0, Subnets.GetHeight() - height() );

  private CidrBlock? Selected {
    get;
    set;
  }

  public void ToggleSelected() {
    lock ( _subnetLock ) {
      var subnet = Subnets.FirstOrDefault( s => s.Cidr == Selected );
      if ( subnet != null ) {
        subnet.IsExpanded = !subnet.IsExpanded;
      }
    }
  }

  public void SelectNext() {
    lock ( _subnetLock ) {
      var subnet = Subnets.FirstOrDefault( s => s.Cidr == Selected );
      if ( subnet == null ) {
        return;
      }

      var index = Subnets.IndexOf( subnet );
      var nextIndex = index + 1;

      if ( nextIndex < Subnets.Count ) {
        Selected = Subnets[nextIndex].Cidr;
      }
    }
  }

  public void SelectPrevious() {
    lock ( _subnetLock ) {
      var subnet = Subnets.FirstOrDefault( s => s.Cidr == Selected );
      if ( subnet == null ) {
        return;
      }

      var index = Subnets.IndexOf( subnet );
      var previousIndex = index - 1;

      if ( previousIndex >= 0 ) {
        Selected = Subnets[previousIndex].Cidr;
      }
    }
  }

  public IEnumerator<Tree> GetEnumerator() {
    lock ( _subnetLock ) {
      var snapshot = Subnets.ToList();
      // TODO Hack; implement IRenderable to provide the right size. Note that other layout changes may make _scrollOffset invalid.
      var offset = Math.Min( _scrollOffset, MaxScrollOffset );
      return TreeRenderer.Render( snapshot, Selected, height(), offset ).GetEnumerator();
    }
  }

  IEnumerator IEnumerable.GetEnumerator() {
    return GetEnumerator();
  }
}