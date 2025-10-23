namespace Drift.Cli.Commands.Agent.Subcommands.Peers.Messages.Adopt;

public class AdoptResponsePayload
{
  public string Status { get; set; }    // "accepted" or "rejected"
  public string AgentId { get; set; }   // Only with "accepted"
  public string Reason { get; set; }    // Only with "rejected"
} 