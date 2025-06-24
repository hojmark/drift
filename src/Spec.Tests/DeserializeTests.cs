using Drift.Spec.Serialization;
using Drift.TestUtilities.ResourceProviders;

namespace Drift.Spec.Tests;

[TestFixture]
public class DeserializeTests {
  [Test]
  public async Task SubnetTest() {
    var stream = LocalTestResourceProvider.GetStream( "network_single_subnet.yaml" );
    var network = YamlConverter.Deserialize( stream );
    await Verify( network );
  }

  [Test]
  public async Task DeviceHostTest() {
    var stream = LocalTestResourceProvider.GetStream( "network_single_device_host.yaml" );
    var network = YamlConverter.Deserialize( stream );
    await Verify( network );
  }

  [Test]
  public async Task ExampelNetworkTest() {
    var stream = LocalTestResourceProvider.GetStream( "network1.yaml" );
    var network = YamlConverter.Deserialize( stream );
    await Verify( network );
  }
}