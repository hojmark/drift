using Drift.Networking.Grpc.Messages;

namespace Drift.Cli.Commands.Agent.Subcommands.Peers.Messages;

public interface IPeerRequest<TResponse> : IPeerMessage where TResponse : IPeerMessage {
}

public interface IPeerResponse : IPeerMessage {
}

public sealed class NoneResponse : IPeerResponse {
  public static readonly NoneResponse Instance = new();

  private NoneResponse() {
  }

  public string MessageType => "none";
}