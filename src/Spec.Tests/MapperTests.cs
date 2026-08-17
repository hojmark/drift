using Drift.Domain.Device.Declared;
using Drift.Spec.Dtos.V1_preview;
using Drift.Spec.Dtos.V1_preview.Mappers;

namespace Drift.Spec.Tests;

internal sealed class MapperTests {
  [Test]
  public void Device_WithoutState_DefaultsToUp() {
    // Arrange
    var dto = new DriftSpec {
      Version = "v1-preview",
      Network = new Network {
        Devices = [
          new Device { Addresses = new Addresses { Ipv4 = "192.168.1.10" } }
        ]
      }
    };

    // Act
    var domain = Mapper.ToDomain( dto );

    // Assert
    Assert.That( domain.Network.Devices, Has.Count.EqualTo( 1 ) );
    Assert.That( domain.Network.Devices[0].State, Is.EqualTo( DeclaredDeviceState.Up ) );
  }

  [Test]
  public void Device_WithExplicitDownState_IsMapped() {
    // Arrange
    var dto = new DriftSpec {
      Version = "v1-preview",
      Network = new Network {
        Devices = [
          new Device { Addresses = new Addresses { Ipv4 = "192.168.1.10" }, State = DeviceState.Down }
        ]
      }
    };

    // Act
    var domain = Mapper.ToDomain( dto );

    // Assert
    Assert.That( domain.Network.Devices[0].State, Is.EqualTo( DeclaredDeviceState.Down ) );
  }

  [Test]
  public void Device_InfoAddresses_AreMappedAsNonIdentity() {
    // Arrange
    var dto = new DriftSpec {
      Version = "v1-preview",
      Network = new Network {
        Devices = [
          new Device {
            Addresses = new Addresses { Ipv4 = "192.168.1.10" }, Info = new Addresses { Hostname = "nas.local" }
          }
        ]
      }
    };

    // Act
    var domain = Mapper.ToDomain( dto );

    // Assert
    var addresses = domain.Network.Devices[0].Addresses;
    Assert.That( addresses, Has.Count.EqualTo( 2 ) );
    Assert.That( addresses.Single( a => a.Value == "192.168.1.10" ).IsId, Is.True );
    Assert.That( addresses.Single( a => a.Value == "nas.local" ).IsId, Is.False );

    // Round-trip: info address must come back under `info:`, not `addresses:`
    var roundTripped = Mapper.ToDto( domain );
    var device = roundTripped.Network.Devices![0];
    Assert.That( device.Addresses.Ipv4, Is.EqualTo( "192.168.1.10" ) );
    Assert.That( device.Addresses.Hostname, Is.Null );
    Assert.That( device.Info?.Hostname, Is.EqualTo( "nas.local" ) );
  }

  [Test]
  public void Server_IsMappedToDomainAndBack() {
    // Arrange
    var dto = new DriftSpec {
      Version = "v1-preview", Network = new Network(), Server = new Server { Address = "http://server:5000" }
    };

    // Act
    var domain = Mapper.ToDomain( dto );
    var roundTripped = Mapper.ToDto( domain );

    // Assert
    Assert.That( domain.Server?.Address, Is.EqualTo( "http://server:5000" ) );
    Assert.That( roundTripped.Server?.Address, Is.EqualTo( "http://server:5000" ) );
  }

  [Test]
  public void AgentPolicy_IsMappedToDomainAndBack() {
    // Arrange
    var dto = new DriftSpec {
      Version = "v1-preview",
      Network = new Network(),
      Agents = [
        new Agent {
          Id = "agentid_test1",
          Address = "http://agent1:5000",
          Policy = [
            new Policy {
              To = ["router"],
              Expect = "reachable",
              Port = [443],
              Protocol = "tcp",
              Probe = ["tls"],
              Fallback = "gateway"
            }
          ]
        }
      ]
    };

    // Act
    var domain = Mapper.ToDomain( dto );
    var roundTripped = Mapper.ToDto( domain );

    // Assert
    var domainPolicy = domain.Agents[0].Policy?[0];
    Assert.That( domainPolicy?.To, Is.EquivalentTo( new[] { "router" } ) );
    Assert.That( domainPolicy?.Expect, Is.EqualTo( "reachable" ) );
    Assert.That( domainPolicy?.Port, Is.EquivalentTo( new[] { 443 } ) );
    Assert.That( domainPolicy?.Protocol, Is.EqualTo( "tcp" ) );
    Assert.That( domainPolicy?.Probe, Is.EquivalentTo( new[] { "tls" } ) );
    Assert.That( domainPolicy?.Fallback, Is.EqualTo( "gateway" ) );

    var roundTrippedPolicy = roundTripped.Agents?[0].Policy?[0];
    Assert.That( roundTrippedPolicy?.To, Is.EquivalentTo( new[] { "router" } ) );
    Assert.That( roundTrippedPolicy?.Expect, Is.EqualTo( "reachable" ) );
  }
}