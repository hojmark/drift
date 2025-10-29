using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.Clustering;

public static class ServiceCollectionExtensions {
  public static void AddClustering( this IServiceCollection services ) {
    services.AddScoped<ICluster, Cluster>();
  }
}