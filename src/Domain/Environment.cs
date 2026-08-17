using System.Text.Json.Serialization;

// TODO remove when no longer a draft
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Drift.Domain;

/*[JsonSerializable( typeof(Environment) )] // Enable source generation for this type
[JsonSourceGenerationOptions( GenerationMode = JsonSourceGenerationMode.Default )]
public partial class EnvironmentContext : JsonSerializerContext {
}*/

public record Environment {
  public required string Name {
    get;
    init;
  }

  public bool Active {
    get;
    set;
  }

  public List<Agent> Agents {
    get;
    set;
  }
}

public record Agent {
  public string Id { // TODO use AgentId???? or should that only be for internal use
    get;
    set;
  }

  /*public IpAddress Address {
    //TODO support hostname too, maybe even mac!?
    get;
    set;
  }*/

  public string Address {
    get;
    set;
  }

  public AgentAuthentication Authentication {
    get;
    set;
  }

  public List<Policy>? Policy {
    get;
    set;
  }
}

/// <summary>
/// A single policy assertion executed by an agent against a target. Not yet executed.
/// </summary>
public record Policy {
  public required List<string> To {
    get;
    init;
  }

  public required string Expect {
    get;
    init;
  }

  public List<int>? Port {
    get;
    init;
  }

  public string? Protocol {
    get;
    init;
  }

  public List<string>? Probe {
    get;
    init;
  }

  public string? Fallback {
    get;
    init;
  }
}

public record AgentAuthentication {
  [JsonIgnore( Condition = JsonIgnoreCondition.Never )]
  public AuthType Type {
    get;
    set;
  }
}

public enum AuthType {
  None = 1,
  ApiKey = 2,
  Certificate =3
}