using System.Text.RegularExpressions;

namespace Drift.Domain;

public sealed partial class AgentId : IEquatable<AgentId>, IParsable<AgentId> {
  private const string Prefix = "agent_";
  private const int MaxSuffixLength = 64;

  private AgentId( string value ) {
    if ( !ValidFormat().IsMatch( value ) ) {
      throw new FormatException(
        $"Agent ID must be '{Prefix}' followed by 1-{MaxSuffixLength} letters, digits, '_', '-' or '.'."
      );
    }

    Value = value;
  }

  public string Value {
    get;
    init;
  } = string.Empty;

  public bool IsGuidBased =>
    Value.Length > Prefix.Length && Guid.TryParse( Value[Prefix.Length..], out _ );

  public Guid? AsGuidOrNull =>
    Value.Length > Prefix.Length && Guid.TryParse( Value[Prefix.Length..], out var guid ) ? guid : null;

  public static implicit operator AgentId( string value ) => Parse( value, null );

  public static implicit operator string( AgentId id ) => id.Value;

  public static AgentId New() => new(Prefix + Guid.NewGuid());

  public static AgentId Parse( string s, IFormatProvider? provider ) => new(s);

  public static bool TryParse( string? s, IFormatProvider? provider, out AgentId result ) {
    try {
      if ( s is null ) {
        result = null!;
        return false;
      }

      result = new AgentId( s );
      return true;
    }
    catch ( FormatException ) {
      result = null!;
      return false;
    }
  }

  public bool Equals( AgentId? other ) =>
    other is not null && string.Equals( Value, other.Value, StringComparison.OrdinalIgnoreCase );

  public override bool Equals( object? obj ) => obj is AgentId other && Equals( other );

  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode( Value );

  public static bool operator ==( AgentId? left, AgentId? right ) =>
    EqualityComparer<AgentId>.Default.Equals( left, right );

  public static bool operator !=( AgentId? left, AgentId? right ) => !( left == right );

  public override string ToString() => Value;

  [GeneratedRegex( @"\Aagent_[A-Za-z0-9_.-]{1,64}\z", RegexOptions.IgnoreCase )]
  private static partial Regex ValidFormat();
}