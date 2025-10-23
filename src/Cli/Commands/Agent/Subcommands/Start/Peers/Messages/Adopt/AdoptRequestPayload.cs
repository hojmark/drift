namespace Drift.Cli.Commands.Agent.Subcommands.Peers.Messages.Adopt;

public class AdoptRequestPayload
{
  public string Jwt { get; set; }
  public string ControllerId { get; set; }
}