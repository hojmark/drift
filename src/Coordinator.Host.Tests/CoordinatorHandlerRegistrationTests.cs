using System.Reflection;
using Drift.Networking.Core.Abstractions;

namespace Drift.Coordinator.Host.Tests;

internal sealed class CoordinatorHandlerRegistrationTests {
  private static readonly Assembly HandlersAssembly = typeof(CoordinatorHost).Assembly;

  [Test]
  public void CoordinatorDoesNotRegisterInboundAgentHandlers() {
    var handlerTypes = HandlersAssembly
      .GetTypes()
      .Where( type => type is { IsAbstract: false, IsInterface: false } )
      .Where( type => typeof(IMessageHandler).IsAssignableFrom( type ) )
      .ToList();

    Assert.That( handlerTypes, Is.Empty );
  }
}
