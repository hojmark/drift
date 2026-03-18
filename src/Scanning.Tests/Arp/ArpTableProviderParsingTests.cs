using System.Net;
using Drift.Domain.Device.Addresses;
using Drift.Scanning.Arp;

namespace Drift.Scanning.Tests.Arp;

// The ParseArpOutput methods are pure string parsing with no OS-specific APIs.
// The [SupportedOSPlatform] attribute on their parent classes exists because those
// classes also spawn OS-specific processes, but the parsing itself is cross-platform.
#pragma warning disable CA1416
internal sealed class ArpTableProviderParsingTests {
  [Test]
  public void WindowsArpTableProvider_ParsesHyphenSeparatedMacs() {
    // Real output from `arp -a` on Windows
    const string output = """
                          Interface: 192.168.1.100 --- 0x7
                            Internet Address      Physical Address      Type
                            192.168.1.1           00-11-22-33-44-55     dynamic
                            192.168.1.2           aa-bb-cc-dd-ee-ff     dynamic
                            224.0.0.22            01-00-5e-00-00-16     static
                          """;

    var table = WindowsArpTableProvider.ParseArpOutput( new StringReader( output ) );

    Assert.That( table.TryGetValue( IPAddress.Parse( "192.168.1.1" ), out var mac1 ), Is.True );
    Assert.That( mac1, Is.EqualTo( new MacAddress( "00-11-22-33-44-55" ) ) );

    Assert.That( table.TryGetValue( IPAddress.Parse( "192.168.1.2" ), out var mac2 ), Is.True );
    Assert.That( mac2, Is.EqualTo( new MacAddress( "AA-BB-CC-DD-EE-FF" ) ) );

    Assert.That( table.TryGetValue( IPAddress.Parse( "224.0.0.22" ), out var mac3 ), Is.True );
    Assert.That( mac3, Is.EqualTo( new MacAddress( "01-00-5E-00-00-16" ) ) );
  }

  [Test]
  public void WindowsArpTableProvider_SkipsHeaderAndBlankLines() {
    const string output = """

                          Interface: 10.0.0.50 --- 0x3

                            Internet Address      Physical Address      Type
                            10.0.0.1              de-ad-be-ef-00-01     dynamic

                          """;

    var table = WindowsArpTableProvider.ParseArpOutput( new StringReader( output ) );

    Assert.That( table.TryGetValue( IPAddress.Parse( "10.0.0.1" ), out var mac ), Is.True );
    Assert.That( mac, Is.EqualTo( new MacAddress( "DE-AD-BE-EF-00-01" ) ) );
  }

  [Test]
  public void LinuxArpTableProvider_ParsesColonSeparatedMacs() {
    // Real output from `arp -en` on Linux
    const string output = """
                          Address          HWtype  HWaddress           Flags Mask  Iface
                          192.168.1.1      ether   00:11:22:33:44:55   C           eth0
                          192.168.1.2      ether   aa:bb:cc:dd:ee:ff   C           eth0
                          """;

    var table = LinuxArpTableProvider.ParseArpOutput( new StringReader( output ) );

    Assert.That( table.TryGetValue( IPAddress.Parse( "192.168.1.1" ), out var mac1 ), Is.True );
    Assert.That( mac1, Is.EqualTo( new MacAddress( "00-11-22-33-44-55" ) ) );

    Assert.That( table.TryGetValue( IPAddress.Parse( "192.168.1.2" ), out var mac2 ), Is.True );
    Assert.That( mac2, Is.EqualTo( new MacAddress( "AA-BB-CC-DD-EE-FF" ) ) );
  }

  [Test]
  public void LinuxArpTableProvider_SkipsHeaderAndBlankLines() {
    const string output = """

                          Address          HWtype  HWaddress           Flags Mask  Iface
                          10.0.0.1         ether   de:ad:be:ef:00:01   C           eth0

                          """;

    var table = LinuxArpTableProvider.ParseArpOutput( new StringReader( output ) );

    Assert.That( table.TryGetValue( IPAddress.Parse( "10.0.0.1" ), out var mac ), Is.True );
    Assert.That( mac, Is.EqualTo( new MacAddress( "DE-AD-BE-EF-00-01" ) ) );
  }
}