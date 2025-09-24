using System.Net.NetworkInformation;
using Drift.Domain;

namespace Drift.Scanning.Subnets.Interface;

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