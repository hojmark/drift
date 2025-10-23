namespace Drift.Cli.Commands.Agent.Subcommands.Peers.Messages.Adopt;

public interface IAdoptRequestHandler {
  public string MessageType => "adopt-request";

  Task HandleAsync( AdoptRequestPayload message, CancellationToken cancellationToken = default );
}