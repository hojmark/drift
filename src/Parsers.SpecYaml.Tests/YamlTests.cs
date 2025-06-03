using Drift.TestUtilities.ResourceProviders;

namespace Drift.Parsers.SpecYaml.Tests;

[TestFixture]
public class YamlTests {
  [Test]
  public void SubnetTest() {
    var stream = LocalTestResourceProvider.GetStream( "network_single_subnet.yaml" );
    var network = YamlConverter.Deserialize( stream );
    Verify( network );
  }

  [Test]
  public void DeviceHostTest() {
    var stream = LocalTestResourceProvider.GetStream( "network_single_device_host.yaml" );
    var network = YamlConverter.Deserialize( stream );
    Verify( network );
  }

  [Test]
  public void ExampelNetworkTest() {
    var stream = LocalTestResourceProvider.GetStream( "network1.yaml" );
    var network = YamlConverter.Deserialize( stream );
    Verify( network );
  }
}