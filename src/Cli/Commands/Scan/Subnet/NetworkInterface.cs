using System.Net.NetworkInformation;
using Drift.Domain;

namespace Drift.Cli.Commands.Scan.Subnet;

public class NetworkInterface : INetworkInterface {
  public required string Description {
    get;
    init;
  }

  public required OperationalStatus OperationalStatus {
    get;
    init;
  }

  public required CidrBlock? UnicastAddress {
    get;
    init;
  }
}