using System.Reflection;
using Drift.Networking.PeerStreaming;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.Cluster;

public static class ServiceCollectionExtensions {
  public static void AddClusterServices( this IServiceCollection services, Assembly ipeermessageassembly ) {
    services.AddPeerStreamingServices( ipeermessageassembly );
    services.AddScoped<ICluster, Cluster>();
  }
}