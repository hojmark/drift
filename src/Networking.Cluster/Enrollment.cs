namespace Drift.Networking.Cluster;

#pragma warning disable CS9113
public class EnrollmentRequest( bool parametersAdoptable, string? parametersJoin ) {
#pragma warning restore CS9113
  public EnrollmentMethod Method => parametersAdoptable ? EnrollmentMethod.Adoption : EnrollmentMethod.Jwt;
}

public enum EnrollmentMethod {
  Adoption,
  Jwt
}