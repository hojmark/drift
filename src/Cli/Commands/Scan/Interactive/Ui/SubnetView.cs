using System.Collections;
using Drift.Cli.Commands.Scan.Models;
using Drift.Domain;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive.Ui;

internal class SubnetView( Func<uint> height ) : IEnumerable<Tree> {
  private readonly Lock _subnetLock = new();
  private List<Subnet> _subnets = [];
  private uint _scrollOffset;

  public List<Subnet> Subnets {
    set {
      lock ( _subnetLock ) {
        _subnets = value;
        if ( _subnets.FirstOrDefault( s => s.Cidr == Selected ) == null ) {
          Selected = _subnets.FirstOrDefault()?.Cidr;
        }

        if ( MaxScrollOffset == 0 ) {
          ScrollOffset = 0;
        }
      }
    }
  }

  public string DebugData {
    get {
      var selectedCidr = _subnets.FirstOrDefault( s => s.Cidr == Selected );
      var selectedIndex = -1;

      if ( selectedCidr != null ) {
        selectedIndex = _subnets.IndexOf( selectedCidr );
      }

      return
        $"{nameof(ScrollOffset)}: {ScrollOffset}, {nameof(MaxScrollOffset)}: {MaxScrollOffset}, TotalHeight: {_subnets.GetHeight()}, ViewportHeight: {height()}, SelectedIndex: {selectedIndex}";
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

  private uint MaxScrollOffset => (uint) Math.Max( 0, _subnets.GetHeight() - height() );

  private CidrBlock? Selected {
    get;
    set;
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

  public IEnumerator<Tree> GetEnumerator() {
    lock ( _subnetLock ) {
      var snapshot = _subnets.ToList();
      // TODO Hack; implement IRenderable to provide the right size. Note that other layout changes may make _scrollOffset invalid.
      var offset = Math.Min( _scrollOffset, MaxScrollOffset );
      return TreeRenderer.Render( snapshot, Selected, height(), offset ).GetEnumerator();
    }
  }

  IEnumerator IEnumerable.GetEnumerator() {
    return GetEnumerator();
  }
}