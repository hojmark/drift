using Drift.Coordinator.Host.Apis.Control;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Coordinator.Host;

public static class ServiceCollectionExtensions {
  /// <summary>
  /// Registers the services used by the server's Control API.
  /// </summary>
  /// <param name="services">The service collection to add Control services to.</param>
  public static void AddControlServices( this IServiceCollection services ) {
    services.AddSingleton<ControlService>();
  }
}