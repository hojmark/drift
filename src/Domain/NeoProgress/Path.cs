namespace Drift.Domain.NeoProgress;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public readonly struct Path : IEquatable<Path>, IEnumerable<string> {
  private readonly string[] _segments;

  public Path( string path ) {
    if ( path == null ) throw new ArgumentNullException( nameof(path) );
    _segments = path.Split( '/', '\\' ).Where( s => !string.IsNullOrEmpty( s ) ).ToArray();
  }

  private Path( string[] segments ) {
    _segments = segments ?? throw new ArgumentNullException( nameof(segments) );
  }

  public static Path operator /( Path left, string right ) {
    if ( string.IsNullOrEmpty( right ) ) throw new ArgumentException( "Segment cannot be null or empty" );
    return new Path( left._segments.Concat( new[] { right } ).ToArray() );
  }

  public static implicit operator string( Path path ) => string.Join( "/", path._segments );
  public static implicit operator Path( string path ) => new(path);

  public override string ToString() => string.Join( "/", _segments );

  public bool Equals( Path other ) => _segments.SequenceEqual( other._segments );
  public override bool Equals( object? obj ) => obj is Path other && Equals( other );
  public override int GetHashCode() => _segments.Aggregate( 0, ( h, s ) => h ^ s.GetHashCode() );

  public IEnumerator<string> GetEnumerator() => ( (IEnumerable<string>) _segments ).GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}