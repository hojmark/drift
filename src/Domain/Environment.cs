using System.Text.Json.Serialization;

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
  public string Id {
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