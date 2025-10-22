using Drift.Cli.Commands.Preview.Agent.Subcommands.Peers;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Grpc.Messages.Adopt;

public class AdoptRequestHandler : HandlerBase<AdoptRequestPayload> /* TODO, IAdoptRequestHandler*/ {
  private readonly ILogger _logger; // Example: inject what you need

  public AdoptRequestHandler(  /*ILogger logger*/ ) {
    //_logger = logger;
  }

  public override string MessageType => "adopt-request2222222";

  protected override Task HandleAsync( AdoptRequestPayload message, CancellationToken cancellationToken = default ) {
    _logger.LogInformation( $"[AdoptRequest] Controller: {message.ControllerId}" );
    return Task.CompletedTask;
  }
}