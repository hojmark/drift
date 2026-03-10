using System.Text.Json.Serialization;

namespace Drift.Domain;

public class AgentId {
  private const string Prefix = "agentid_";

  [JsonConstructor]
  public AgentId() {
  }

  public AgentId( string value ) {
    if ( !value.StartsWith( Prefix ) ) {
      throw new FormatException( $"AgentId must start with '{Prefix}'." );
    }

    Value = value;
  }

  public string Value {
    get;
    set;
  } = string.Empty;

  public bool IsGuidBased =>
    Guid.TryParse( Value[Prefix.Length..], out _ );

  public Guid? AsGuidOrNull =>
    Guid.TryParse( Value[Prefix.Length..], out var guid ) ? guid : null;

  public static implicit operator AgentId( string value ) => new AgentId( value );

  public static implicit operator string( AgentId id ) => id.Value;

  public static AgentId New() => new AgentId( Prefix + Guid.NewGuid() );

  public override string ToString() => Value;
}