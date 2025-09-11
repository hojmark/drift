namespace Drift.Cli.Commands.Scan.Interactive;

public class Subnet {
  public string Address { get; }
  public List<Device> Devices { get; }

  public Subnet(string address, List<Device> devices) {
    Address = address;
    Devices = devices;
  }

  public static List<Subnet> Sample() => new() {
    new Subnet("192.168.1.0/24", new() {
      new Device("192.168.1.10", "AA:BB:CC:DD:EE:01", true),
      new Device("192.168.1.11", "AA:BB:CC:DD:EE:02", false),
      new Device("192.168.1.12", "AA:BB:CC:DD:EE:03", true),
    }),
    new Subnet("10.0.0.0/24", new() {
      new Device("10.0.0.1", "FF:EE:DD:CC:BB:01", true),
      new Device("10.0.0.2", "FF:EE:DD:CC:BB:02", true),
    }),
  };
}

public class Device {
  public string IP { get; }
  public string MAC { get; }
  public bool IsOnline { get; }

  public Device(string ip, string mac, bool isOnline) {
    IP = ip;
    MAC = mac;
    IsOnline = isOnline;
  }
}
