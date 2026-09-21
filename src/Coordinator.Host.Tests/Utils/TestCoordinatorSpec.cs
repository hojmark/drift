using Drift.Coordinator.Services.Spec;
using Drift.Domain;

namespace Drift.Coordinator.Host.Tests.Utils;

internal static class TestCoordinatorSpec {
  public static CoordinatorSpec Create( bool isDeclared ) {
    return new CoordinatorSpec( new Inventory {
      Network = new(),
      Agents = isDeclared
        ? [new Drift.Domain.Agent { Id = "agent_one", Address = "http://127.0.0.1:5001" }]
        : []
    } );
  }
}
