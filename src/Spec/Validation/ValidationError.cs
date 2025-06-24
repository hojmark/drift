namespace Drift.Spec.Validation;

public class ValidationError {
  public string Path {
    get;
    set;
  } = string.Empty;

  public string Message {
    get;
    set;
  } = string.Empty;

  public string SchemaPath {
    get;
    set;
  } = string.Empty;

  public override string ToString() {
    return $"{Path}: {Message}";
  }
}