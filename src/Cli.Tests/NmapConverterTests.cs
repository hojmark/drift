/*using Drift.Parsers.EnvironmentJson;
using Drift.Parsers.NmapXml;
using Drift.TestUtilities.ResourceProviders;
using NmapXmlParser;

namespace Drift.Cli.Tests;

public class NmapConverterTests {
  [Test]
  public void HostTest() {
    var nmaprun = new nmaprun {
      Items = [
        new host {
          Items = [new ports { port = [new port { portid = "22" }] }],
          address = new address { addr = "192.168.0.10" },
          status = new status { state = statusState.up }
        }
      ]
    };

    var devices = NmapConverter.ToDevices( nmaprun );

    Verify( JsonConverter.Serialize( devices ) );
  }

  //TODO implement
  [Explicit]
  [Test]
  public void Demo0XmlTest() {
    var demo0Xml = SharedTestResourceProvider.GetStream( "nmap_demo0.xml" );
    var nmaprun = NmapXmlReader.Deserialize( demo0Xml );

    var snapshot = NmapConverter.ToDevices( nmaprun );

    Verify( JsonConverter.Serialize( snapshot ) );
  }
}*/

