using Drift.Networking.Core.Abstractions;
using Drift.Networking.Core.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.Core;

public static class ServiceCollectionExtensions {
  /// <summary>
  /// Registers the core services used to serialize, dispatch, correlate, and manage gRPC messaging streams.
  /// </summary>
  /// <remarks>
  /// Set <see cref="MessagingOptions.MessageAssembly"/> to the assembly containing the protocol message types.
  /// </remarks>
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