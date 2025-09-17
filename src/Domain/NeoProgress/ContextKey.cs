namespace Drift.Domain.NeoProgress;

public readonly record struct ContextKey<T>( string Name ) {
  public static implicit operator string( ContextKey<T> key ) => key.Name;
  public override string ToString() => Name;
}