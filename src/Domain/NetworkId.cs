namespace Drift.Domain;

public record NetworkId( string Value ) {
  public override string ToString() {
    return Value;
  }
}