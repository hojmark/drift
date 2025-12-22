using System.Reflection;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Agent.PeerProtocol.Tests;

internal sealed class PeerMessageHandlerTests {
  private static readonly Assembly ProtocolAssembly = typeof(PeerProtocolAssemblyMarker).Assembly;
  private static readonly IEnumerable<Type> RequestTypes = GetAllConcreteMessageTypes( typeof(IPeerRequest<>) );
  private static readonly IEnumerable<Type> ResponseTypes = GetAllConcreteMessageTypes( typeof(IPeerResponse) );
  private static readonly IEnumerable<Type> HandlerTypes = GetAllConcreteHandlerTypes();

  [Test]
  public void FindMessagesAndHandlersAndMessages() {
    using var _ = Assert.EnterMultipleScope();
    Assert.That( RequestTypes.ToList(), Has.Count.GreaterThan( 1 ), "No request messages found via reflection" );
    Assert.That( ResponseTypes.ToList(), Has.Count.GreaterThan( 1 ), "No response messages found via reflection" );
    Assert.That( HandlerTypes.ToList(), Has.Count.GreaterThan( 1 ), "No handlers found via reflection" );
  }

  [Test]
  public void AllRequestMessagesHaveHandlers_AndNoExtraHandlers() {
    var handledRequestTypes = HandlerTypes
      .Select( t => t.GetInterfaces()
          .First( i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPeerMessageHandler<,>) )
          .GetGenericArguments()[0] // 1st generic parameter = TRequest
      )
      .ToList();

    var requestsWithoutHandler = RequestTypes
      .Except( handledRequestTypes )
      .Select( t => t.Name )
      .ToList();

    var extraHandlers = handledRequestTypes
      .Except( RequestTypes )
      .Select( t => t.Name )
      .ToList();

    Assert.That(
      requestsWithoutHandler,
      Is.Empty,
      "Request messages without a handler: " + string.Join( ", ", requestsWithoutHandler )
    );

    Assert.That(
      extraHandlers,
      Is.Empty,
      "Handlers for unknown request messages: " + string.Join( ", ", extraHandlers )
    );
  }

  [Test]
  public void AllResponseMessagesHaveHandlers_AndNoExtraHandlers() {
    var handledResponseTypes = HandlerTypes
      .Select( t => t.GetInterfaces()
        .First( i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPeerMessageHandler<,>) )
        .GetGenericArguments()[1] ) // 2nd generic parameter = TResponse
      .ToList();

    var responsesWithoutHandler = ResponseTypes
      .Except( handledResponseTypes )
      .Select( t => t.Name )
      .ToList();

    var extraHandlers = handledResponseTypes
      .Except( ResponseTypes )
      .Select( t => t.Name )
      .ToList();

    Assert.That(
      responsesWithoutHandler,
      Is.Empty,
      "Response messages without a handler: " + string.Join( ", ", responsesWithoutHandler )
    );

    Assert.That(
      extraHandlers,
      Is.Empty,
      "Handlers for unknown response messages: " + string.Join( ", ", extraHandlers )
    );
  }

  [Explicit( "Disabled until interface has settled" )]
  [TestCaseSource( nameof(RequestTypes) )]
  [TestCaseSource( nameof(ResponseTypes) )]
  public void Messages_HaveValidMessageTypeAndJsonInfo( Type type ) {
    var messageTypeValue = type
      .GetProperty( nameof(IPeerMessage.MessageType), BindingFlags.Public | BindingFlags.Static )!
      .GetValue( null ) as string;

    Assert.That( messageTypeValue, Is.Not.Null.And.Not.Empty );

    var jsonInfoValue = type
      .GetProperty( nameof(IPeerMessage.JsonInfo), BindingFlags.Public | BindingFlags.Static )!
      .GetValue( null );

    Assert.That( jsonInfoValue, Is.Not.Null );
  }

  private static List<Type> GetAllConcreteMessageTypes( Type baseType ) {
    return ProtocolAssembly
      .GetTypes()
      .Concat( [typeof(Empty)] )
      .Where( t => t is { IsAbstract: false, IsInterface: false } )
      .Where( t =>
        // Generic base type
        t.GetInterfaces().Any( i => i.IsGenericType && i.GetGenericTypeDefinition() == baseType ) ||
        // Non-generic base type
        baseType.IsAssignableFrom( t )
      )
      .ToList();
  }

  private static List<Type> GetAllConcreteHandlerTypes() {
    return ProtocolAssembly
      .GetTypes()
      .Where( t => t is { IsAbstract: false, IsInterface: false } )
      .Where( t => t.GetInterfaces()
        .Any( i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPeerMessageHandler<,>) ) )
      .ToList();
  }
}