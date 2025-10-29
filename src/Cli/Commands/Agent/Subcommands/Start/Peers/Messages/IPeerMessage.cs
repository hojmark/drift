using Drift.Networking.PeerStreaming.Core.Messages;

namespace Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages;

public interface IPeerRequest<TResponse> : IPeerMessage where TResponse : IPeerMessage {
}

public interface IPeerResponse : IPeerMessage {
}

public sealed class NoneResponse : IPeerResponse {
  public static readonly NoneResponse Instance = new();

  // TODO make private
  public NoneResponse() {
  }

  public string MessageType => "none";
}