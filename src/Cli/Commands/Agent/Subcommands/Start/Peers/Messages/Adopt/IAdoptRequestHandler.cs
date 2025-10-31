namespace Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Adopt;

public interface IAdoptRequestHandler {
  public string MessageType => "adopt-request";

  Task HandleAsync( AdoptRequestPayload message, CancellationToken cancellationToken = default );
}