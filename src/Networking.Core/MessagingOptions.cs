using System.Reflection;

namespace Drift.Networking.Core;

public sealed class MessagingOptions {
  public CancellationToken StoppingToken {
    get;
    set;
  }

  public Assembly MessageAssembly {
    get;
    init;
  } = Assembly.GetExecutingAssembly();
}