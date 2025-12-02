namespace Drift.Networking.Clustering;

public class EnrollmentRequest( bool parametersAdoptable, string? parametersJoin ) {
  public EnrollmentMethod Method => parametersAdoptable ? EnrollmentMethod.Adoption : EnrollmentMethod.Jwt;
}

public enum EnrollmentMethod {
  Adoption,
  Jwt
}