namespace Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Adopt;

public class AdoptRequestHandler : HandlerBase<AdoptRequestPayload> /* TODO, IAdoptRequestHandler*/ {
  private readonly ILogger _logger; // Example: inject what you need

  public override string MessageType => "adopt-request2222222";

  protected override Task HandleAsync( AdoptRequestPayload message, CancellationToken cancellationToken = default ) {
    _logger.LogInformation( $"[AdoptRequest] Controller: {message.ControllerId}" );
    return Task.CompletedTask;
  }
}