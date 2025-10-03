using Spectre.Console.Rendering;

namespace Drift.Cli.Commands.Scan.Interactive.Ui;

internal sealed class LogView( Func<uint> height ) : IRenderable {
  private readonly List<string> _logLines = [];
  private bool _autoScroll = true;
  private uint _scrollOffset;

  // Note: allow setting negative values; values outside the range will be clamped
  internal int ScrollOffset {
    get {
      return (int) _scrollOffset;
    }

    set {
      _scrollOffset = (uint) Math.Clamp( value, 0, MaxScrollOffset );
      if ( _scrollOffset == MaxScrollOffset ) {
        _autoScroll = true;
      }
    }
  }

  // private uint MaxScrollOffset => (uint) Math.Max( 0, _subnets.GetHeight() - height() );
  private uint MaxScrollOffset => (uint) Math.Max( 0, _logLines.Count - height() );

  public Measurement Measure( RenderOptions options, int maxWidth ) {
    return new Measurement( 1, 1000 );
  }

  public IEnumerable<Segment> Render( RenderOptions options, int maxWidth ) {
    return _logLines.Skip( ScrollOffset ).Select( l => new Segment( l + Environment.NewLine ) );
  }

  internal void AddLine( string line ) {
    _logLines.Add( line );
    if ( _autoScroll ) {
      ScrollOffset++;
    }
  }
}