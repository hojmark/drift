using Drift.Coordinator.Api.Control;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Coordinator.Api;

public static class CoordinatorApiExtensions {
  public static IServiceCollection AddCoordinatorApi( this IServiceCollection services ) {
    services.AddSingleton<CoordinatorApiMarker>();
    services.AddOpenApi( "v1", options => options.AddDocumentTransformer( new CoordinatorOpenApiTransformer() ) );
    services.ConfigureHttpJsonOptions( options =>
      options.SerializerOptions.TypeInfoResolverChain.Insert( 0, JsonSerializerContext.Default )
    );
    services.AddTransient<IStartupFilter, CoordinatorApiValidationFilter>();
    return services;
  }

  public static IEndpointRouteBuilder MapCoordinatorApi( this IEndpointRouteBuilder endpoints ) {
    var marker = endpoints.ServiceProvider.GetService<CoordinatorApiMarker>();
    if ( marker == null ) {
      throw new InvalidOperationException(
        $"Unable to find the required services. Add them by calling '{nameof(IServiceCollection)}.{nameof(AddCoordinatorApi)}'."
      );
    }

    endpoints.MapControlApi();
    marker.EndpointsMapped = true;
    return endpoints;
  }
}

internal sealed class CoordinatorApiMarker {
  internal bool EndpointsMapped {
    get;
    set;
  }
}

internal sealed class CoordinatorApiValidationFilter : IStartupFilter {
  public Action<IApplicationBuilder> Configure( Action<IApplicationBuilder> next ) {
    return app => {
      next( app );

      var marker = app.ApplicationServices.GetRequiredService<CoordinatorApiMarker>();
      if ( !marker.EndpointsMapped ) {
        throw new InvalidOperationException(
          $"API endpoints were not mapped. Map them by calling '{nameof(IEndpointRouteBuilder)}.{nameof(CoordinatorApiExtensions.MapCoordinatorApi)}'."
        );
      }
    };
  }
}