namespace Drift.Cli.Commands;

internal sealed class NestedHostLifetime {
  public TaskCompletionSource Ready {
    get;
  } = new();
}