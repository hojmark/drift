namespace Drift.Scanning.Subnets.Interface;

public interface IInterfaceSubnetProvider : ISubnetProvider {
  List<INetworkInterface> GetInterfaces();
}