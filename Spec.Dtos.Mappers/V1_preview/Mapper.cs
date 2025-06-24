using Dto = Drift.Spec.Dtos.V1_preview;

namespace Drift.Spec.Dtos.Mappers.V1_preview;

public class Mapper {
  public static Domain.Inventory ToDomain( Dto.DriftSpec dto ) {
    return new Domain.Inventory { Network = Map( dto.Network ) };
  }

  private static Domain.Network Map( Dto.Network dtoNetwork ) {
    return new Domain.Network {
      Id = dtoNetwork.Id, Subnets = Map( dtoNetwork.Subnets ), Devices = Map( dtoNetwork.Devices )
    };
  }

  private static List<Domain.DeclaredSubnet> Map( List<Dto.Subnet> subnets ) {
    return subnets.Select( Map ).ToList();
  }

  private static Domain.DeclaredSubnet Map( Dto.Subnet subnet ) {
    // TODO incomplete
    return new Domain.DeclaredSubnet { Id = subnet.Id };
  }

  private static List<Domain.Device.Declared.DeclaredDevice> Map( List<Dto.Device> subnets ) {
    return subnets.Select( Map ).ToList();
  }

  private static Domain.Device.Declared.DeclaredDevice Map( Dto.Device subnet ) {
    // TODO incomplete
    return new Domain.Device.Declared.DeclaredDevice { Id = subnet.Id };
  }
}