namespace Drift.Domain;

public readonly record struct Percentage {
  public static readonly Percentage Zero = new(0);
  public static readonly Percentage Hundred = new(100);

  public byte Value {
    get;
  }

  public Percentage( byte value ) {
    if ( value > 100 ) {
      throw new ArgumentOutOfRangeException( nameof(value), "Percentage must be between 0 and 100." );
    }

    Value = value;
  }

  public override string ToString() => $"{Value}%";

  public static implicit operator byte( Percentage p ) => p.Value;
  public static explicit operator Percentage( byte value ) => new(value);
}