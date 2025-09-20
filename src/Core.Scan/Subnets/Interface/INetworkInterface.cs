using System.Net.NetworkInformation;
using Drift.Domain;

namespace Drift.Core.Scan.Subnets.Interface;

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