using Drift.Agent.Host.Subnets;
using Drift.Agent.Host.Tests.Utils;
using Drift.Domain;
using Drift.Messaging.Protocol.Agent.Subnets;
using Drift.Scanning.Subnets;
using Drift.Scanning.Subnets.Interface;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Agent.Host.Tests.Subnets;

internal sealed class InterfaceSubnetsRequestHandlerTests {
  [Test]
  public async Task ReturnsInterfaceSubnets() {
    var cidr = new CidrBlock( "192.168.1.0/24" );
    var provider = new StubInterfaceSubnetProvider( [new ResolvedSubnet( cidr, SubnetSource.Local )] );
    var responder = new RecordingMessageResponder<SubnetsResponse>();

    await new InterfaceSubnetsRequestHandler( provider, NullLogger.Instance ).HandleAsync(
      new SubnetsRequest(),
      responder,
      CancellationToken.None
    );

    Assert.That( responder.Responses.Single().Subnets, Is.EqualTo( [cidr] ) );
  }

  private sealed class StubInterfaceSubnetProvider( List<ResolvedSubnet> subnets ) : IInterfaceSubnetProvider {
    public Task<List<ResolvedSubnet>> GetAsync() => Task.FromResult( subnets );
    public List<INetworkInterface> GetInterfaces() => [];
  }
}