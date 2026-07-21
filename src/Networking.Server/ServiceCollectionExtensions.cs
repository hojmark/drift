using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.Server;

public static class ServiceCollectionExtensions {
  public static void AddMessagingServer(
    this IServiceCollection services,
    Action<GrpcServiceOptions>? configureOptions = null
  ) {
    services.AddSingleton<MessagingServerMarker>();
    services.AddGrpc( options => configureOptions?.Invoke( options ) );
    services.AddTransient<IStartupFilter, MessagingServerValidationFilter>();
  }

  public static void MapMessagingServerEndpoints( this IEndpointRouteBuilder app ) {
    var marker = app.ServiceProvider.GetService<MessagingServerMarker>();

    if ( marker == null ) {
      throw new InvalidOperationException(
        $"Unable to find the required services. Add them by calling '{nameof(IServiceCollection)}.{nameof(AddMessagingServer)}'."
      );
    }

    app.MapGrpcService<InboundMessageService>();

    marker.EndpointsMapped = true;
  }
}

internal sealed class MessagingServerValidationFilter : IStartupFilter {
  public Action<IApplicationBuilder> Configure( Action<IApplicationBuilder> next ) {
    return app => {
      next( app );

      var marker = app.ApplicationServices.GetRequiredService<MessagingServerMarker>();

      if ( !marker.EndpointsMapped ) {
        throw new InvalidOperationException(
          $"Server endpoints were not mapped. Map them by calling '{nameof(IEndpointRouteBuilder)}.{nameof(ServiceCollectionExtensions.MapMessagingServerEndpoints)}'." );
      }
    };
  }
}
