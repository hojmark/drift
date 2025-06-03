namespace Drift.Domain.Device.Addresses;

/**

Layer	Name	Purpose / Function	Examples
7	Application	User-facing apps, APIs	HTTP, FTP, DNS, SSH, SMTP, TLS, SNMP
6	Presentation	Data translation, encryption, compression	SSL/TLS, JPEG, ASCII, MPEG
5	Session	Session control, authentication, dialogs	NetBIOS, RPC, SMB Session
4	Transport	Reliable delivery, flow control, segmentation	TCP, UDP
3	Network	Routing, addressing, packet forwarding	IP, ICMP, ARP, BGP, OSPF
2	Data Link	Frames, MAC addressing, switching	Ethernet, Wi-Fi (802.11), PPP, VLAN
1	Physical	Raw bits, hardware transmission	Cables, NICs, hubs, electrical signals
 */
/// <summary>
/// So far just a marker interface
/// </summary>
public interface IDeviceAddress {
  AddressType Type {
    get;
  }

  string Value {
    get;
  }

  bool? IsId {
    get;
  }

  // TODO introduce required
  /*public bool Required {
    get;
  }*/
}