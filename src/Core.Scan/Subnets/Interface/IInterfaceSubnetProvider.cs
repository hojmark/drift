namespace Drift.Core.Scan.Subnets.Interface;

public interface IInterfaceSubnetProvider : ISubnetProvider {
  List<INetworkInterface> GetInterfaces();
}