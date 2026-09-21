namespace Drift.Domain;

public record RequestId( Guid Value ) {
  private const string Prefix = "requestid_";

  public static RequestId Parse( string value ) {
    if ( !value.StartsWith( Prefix ) ) {
      throw new FormatException( $"Invalid RequestId format. Must start with '{Prefix}'." );
    }

    var guidPart = value[Prefix.Length..];

    if ( !Guid.TryParse( guidPart, out var guid ) ) {
      throw new FormatException( "Invalid GUID in RequestId." );
    }

    return new RequestId( guid );
  }

  public override string ToString() => $"{Prefix}{Value}";

  public static RequestId New() => new(Guid.NewGuid());
}