using System.Net;
using Drift.Domain.Device.Addresses;
using Drift.Scanning.Arp;

namespace Drift.Scanning.Tests.Arp;

internal sealed class ArpTableProviderParsingTests {
  [Test]
  [Platform( "Win" )]
  public void WindowsArtpTableProvider_DoesntThrow() {
    // Arrange
    var provider = new WindowsArpTableProvider();

    // Act / Assert
    Assert.DoesNotThrow( () => _ = provider.Fresh );
    Assert.IsNotNull( provider.Cached );
    Assert.That( provider.Cached, Is.Not.Empty );

    Console.WriteLine( provider.Cached.First() );
  }

  [Test]
  public void WindowsArpTableProvider_Parses() {
    const string output = """
                          Interface: 192.168.1.100 --- 0x7
                            Internet Address      Physical Address      Type
                            192.168.1.1           00-11-22-33-44-55     dynamic
                            192.168.1.2           aa-bb-cc-dd-ee-ff     dynamic
                            224.0.0.22            01-00-5e-00-00-16     static
                          """;

    var table = WindowsArpTableProvider.ParseArpOutput( new StringReader( output ) );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( table.TryGetValue( IPAddress.Parse( "192.168.1.1" ), out var mac1 ), Is.True );
      Assert.That( mac1, Is.EqualTo( new MacAddress( "00-11-22-33-44-55" ) ) );

      Assert.That( table.TryGetValue( IPAddress.Parse( "192.168.1.2" ), out var mac2 ), Is.True );
      Assert.That( mac2, Is.EqualTo( new MacAddress( "AA-BB-CC-DD-EE-FF" ) ) );

      Assert.That( table.TryGetValue( IPAddress.Parse( "224.0.0.22" ), out var mac3 ), Is.True );
      Assert.That( mac3, Is.EqualTo( new MacAddress( "01-00-5E-00-00-16" ) ) );
    }
  }

  [Test]
  [Platform( "Linux" )]
  public void LinuxArpTableProvider_DoesntThrow() {
    // Arrange
    var provider = new LinuxArpTableProvider();

    // Act / Assert
    Assert.DoesNotThrow( () => _ = provider.Fresh );
    Assert.IsNotNull( provider.Cached );
    Assert.That( provider.Cached, Is.Not.Empty );

    Console.WriteLine( provider.Cached.First() );
  }

  [Test]
  public void LinuxArpTableProvider_Parses() {
    const string output = """
                          IP address       HW type     Flags       HW address            Mask     Device
                          192.168.1.1      0x1         0x2         00:11:22:33:44:55     *        eth0
                          192.168.1.2      0x1         0x2         aa:bb:cc:dd:ee:ff     *        eth0
                          """;

    var table = LinuxArpTableProvider.ParseArpOutput( new StringReader( output ) );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( table.TryGetValue( IPAddress.Parse( "192.168.1.1" ), out var mac1 ), Is.True );
      Assert.That( mac1, Is.EqualTo( new MacAddress( "00-11-22-33-44-55" ) ) );

      Assert.That( table.TryGetValue( IPAddress.Parse( "192.168.1.2" ), out var mac2 ), Is.True );
      Assert.That( mac2, Is.EqualTo( new MacAddress( "AA-BB-CC-DD-EE-FF" ) ) );
    }
  }
}