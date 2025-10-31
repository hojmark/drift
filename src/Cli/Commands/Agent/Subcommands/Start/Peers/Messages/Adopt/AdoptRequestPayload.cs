namespace Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Adopt;

public class AdoptRequestPayload
{
  public string Jwt { get; set; }
  public string ControllerId { get; set; }
}