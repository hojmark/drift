using System.Reflection;

namespace Drift.Networking.PeerStreaming.Core;

public sealed class PeerStreamingOptions {
  public CancellationToken StoppingToken {
    get;
    set;
  }

  public Assembly MessageAssembly {
    get;
    init;
  } = Assembly.GetExecutingAssembly();
}