namespace Drift.Cli.Commands.Scan.Subnet;

public interface IInterfaceSubnetProvider : ISubnetProvider {
  List<INetworkInterface> GetInterfaces();
}