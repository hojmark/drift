namespace Drift.Spec.Dtos.V1_preview.Mappers;

public class Mapper {
  public static Domain.Inventory ToDomain( DriftSpec dto ) {
    return new Domain.Inventory { Network = Map( dto.Network ) };
  }

  private static Domain.Network Map( Network dtoNetwork ) {
    return new Domain.Network {
      Id = dtoNetwork.Id, Subnets = Map( dtoNetwork.Subnets ), Devices = Map( dtoNetwork.Devices )
    };
  }

  private static List<Domain.DeclaredSubnet> Map( List<Subnet> subnets ) {
    return subnets.Select( Map ).ToList();
  }

  private static Domain.DeclaredSubnet Map( Subnet subnet ) {
    // TODO incomplete
    return new Domain.DeclaredSubnet { Id = subnet.Id };
  }

  private static List<Domain.Device.Declared.DeclaredDevice> Map( List<Device> subnets ) {
    return subnets.Select( Map ).ToList();
  }

  private static Domain.Device.Declared.DeclaredDevice Map( Device subnet ) {
    // TODO incomplete
    return new Domain.Device.Declared.DeclaredDevice { Id = subnet.Id };
  }
}