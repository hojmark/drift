namespace Drift.Domain;

public record Inventory {
  public required Network Network {
    get;
    init;
  }

  public Server? Server {
    get;
    set;
  }

  public Settings? Settings {
    get;
    set;
  }

  public List<Agent> Agents {
    get;
    set;
  } = [];
}

public record Server {
  // TODO Use Uri type
  public required string Address {
    get;
    init;
  }
}