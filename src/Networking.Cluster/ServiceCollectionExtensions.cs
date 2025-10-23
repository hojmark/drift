using System.Reflection;
using Drift.Networking.Peer;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.Cluster;

public static class ServiceCollectionExtensions {
  public static void AddClusterServices( this IServiceCollection services, Assembly ipeermessageassembly ) {
    services.AddPeerServices( ipeermessageassembly );
    services.AddScoped<ICluster, Cluster>();
  }
}