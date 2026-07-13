using System.Reflection;
using Drift.Messaging.Protocol;
using Drift.Networking.Core.Abstractions;

namespace Drift.Messaging.Tests;

internal sealed class MessageHandlerTests {
  private static readonly Assembly MessagesAssembly = typeof(ProtocolMessagesAssemblyMarker).Assembly;
  private static readonly IEnumerable<Type> RequestTypes = GetAllConcreteMessageTypes( typeof(IRequest<>) );
  private static readonly IEnumerable<Type> ResponseTypes = GetAllConcreteMessageTypes( typeof(IResponse) );

  [Test]
  public void FindMessagesAndHandlers() {
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( RequestTypes.ToList(), Has.Count.GreaterThan( 1 ), "No request messages found via reflection" );
      Assert.That( ResponseTypes.ToList(), Has.Count.GreaterThan( 1 ), "No response messages found via reflection" );
    }
  }

  [TestCaseSource( nameof(RequestTypes) )]
  [TestCaseSource( nameof(ResponseTypes) )]
  public void Messages_HaveValidMessageTypeAndJsonInfo( Type type ) {
    // Arrange / Act
    var messageTypeValue = type
      .GetProperty( nameof(IMessage.MessageType), BindingFlags.Public | BindingFlags.Static )!
      .GetValue( null ) as string;

    var jsonInfoValue = type
      .GetProperty( nameof(IMessage.JsonInfo), BindingFlags.Public | BindingFlags.Static )!
      .GetValue( null );

    // Assert
    Assert.That( messageTypeValue, Is.Not.Null.And.Not.Empty );
    Assert.That( jsonInfoValue, Is.Not.Null );
  }

  private static List<Type> GetAllConcreteMessageTypes( Type baseType ) {
    return MessagesAssembly
      .GetTypes()
      // .Concat( [typeof(Empty)] ) // TODO what about Empty?
      .Where( t => t is { IsAbstract: false, IsInterface: false } )
      .Where( t =>
        // Generic base type
        t.GetInterfaces().Any( i => i.IsGenericType && i.GetGenericTypeDefinition() == baseType ) ||
        // Non-generic base type
        baseType.IsAssignableFrom( t )
      )
      .ToList();
  }
}