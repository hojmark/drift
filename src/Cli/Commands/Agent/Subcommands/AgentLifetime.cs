namespace Drift.Cli.Commands.Agent.Subcommands;

internal sealed class AgentLifetime {
  public TaskCompletionSource Ready {
    get;
  } = new();
}