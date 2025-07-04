namespace Drift.Spec.Validation;

public class ValidationError {
  public string Path {
    get;
    init;
  } = string.Empty;

  public string Message {
    get;
    init;
  } = string.Empty;

  public string SchemaPath {
    get;
    init;
  } = string.Empty;

  public override string ToString() {
    return $"{Path}: {Message}";
  }
}