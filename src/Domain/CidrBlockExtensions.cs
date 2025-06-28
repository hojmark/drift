namespace Drift.Domain;

public static class CidrBlockExtensions {
  public static DeclaredSubnet ToDeclared( this CidrBlock cidrBlock ) {
    return new DeclaredSubnet { Address = cidrBlock.ToString() };
  }

  public static List<DeclaredSubnet> ToDeclared( this IEnumerable<CidrBlock> cidrBlocks ) {
    return cidrBlocks.Select( ToDeclared ).ToList();
  }
}