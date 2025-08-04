using System.Net.NetworkInformation;
using Drift.Domain;

namespace Drift.Cli.Commands.Scan.Subnet;

public interface INetworkInterface {
  string Description {
    get;
  }

  OperationalStatus OperationalStatus {
    get;
  }

  CidrBlock? UnicastAddress {
    get;
  }
}