using System.Text.Json.Serialization;

namespace Drift.Domain;

public class AgentId {
  private const string Prefix = "agentid_";

  public string Value {
    get;
    set;
  }

  [JsonConstructor]
  public AgentId() {
  }

  public AgentId( string value ) {
    if ( !value.StartsWith( Prefix ) )
      throw new FormatException( $"AgentId must start with '{Prefix}'." );

    Value = value;
  }

  public static AgentId New() => new AgentId( Prefix + Guid.NewGuid() );

  public static implicit operator AgentId( string value ) => new AgentId( value );

  public static implicit operator string( AgentId id ) => id.Value;

  public bool IsGuidBased =>
    Guid.TryParse( Value[Prefix.Length..], out _ );

  public Guid? AsGuidOrNull =>
    Guid.TryParse( Value[Prefix.Length..], out var guid ) ? guid : null;

  public override string ToString() => Value;
}