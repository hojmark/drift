namespace Drift.Cli.Commands.Scan.Interactive;

public class Subnet {
  public string Address {
    get;
  }

  public List<Device> Devices {
    get;
  }

  public Subnet( string address, List<Device> devices ) {
    Address = address;
    Devices = devices;
  }
}

public class Device {
  public string IP {
    get;
  }

  public string MAC {
    get;
  }

  public bool IsOnline {
    get;
  }

  public Device( string ip, string mac, bool isOnline ) {
    IP = ip;
    MAC = mac;
    IsOnline = isOnline;
  }
}