namespace Drift.Core.Scan.Subnet;

public interface IInterfaceSubnetProvider : ISubnetProvider {
  List<INetworkInterface> GetInterfaces();
}