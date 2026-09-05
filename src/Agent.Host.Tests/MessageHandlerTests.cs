using System.Reflection;
using Drift.Networking.Core.Abstractions;

namespace Drift.Agent.Host.Tests;

// TODO currently not doing anything useful
// TODO almost duplicate of MessageHandlerTests in Coordinator.Host.Tests
internal sealed class MessageHandlerTests {
  private static readonly Assembly HandlersAssembly = typeof(AgentHost).Assembly;
  private static readonly IEnumerable<Type> HandlerTypes = GetAllConcreteHandlerTypes();

  [Test]
  public void FindMessagesAndHandlers() {
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( HandlerTypes.ToList(), Has.Count.GreaterThan( 1 ), "No handlers found via reflection" );
    }
  }

  private static List<Type> GetAllConcreteHandlerTypes() {
    return HandlersAssembly
      .GetTypes()
      .Where( t => t is { IsAbstract: false, IsInterface: false } )
      .Where( t => typeof(IMessageHandler).IsAssignableFrom( t ) )
      .ToList();
  }
}