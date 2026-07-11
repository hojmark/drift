using Drift.Networking.Core.Abstractions;
using Drift.Networking.Core.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.Core;

public static class ServiceCollectionExtensions {
  public static void AddMessagingCore(
    this IServiceCollection services,
    MessagingOptions options
  ) {
    services.AddSingleton( options );
    services.AddSingleton<IMessageEnvelopeConverter, MessageEnvelopeConverter>();
    services.AddScoped<MessageDispatcher>();
    services.AddScoped<MessageResponseCorrelator>();
    services.AddScoped<IMessageStreamManager, MessageStreamManager>();
  }
}