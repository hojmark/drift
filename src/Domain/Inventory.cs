namespace Drift.Domain;

public record Inventory {
  public required Network Network {
    get;
    init;
  }

  public List<Agent> Agents {
    get;
    set;
  } = [];
}