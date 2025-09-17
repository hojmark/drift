namespace Drift.Core.Scan.Subnet.Interface;

public interface IInterfaceSubnetProvider : ISubnetProvider {
  List<INetworkInterface> GetInterfaces();
}