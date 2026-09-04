using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Drift.Coordinator.Host.Apis.Control;

internal static class EndpointRouteBuilderExtensions {
  extension( IEndpointRouteBuilder endpoints ) {
    public void MapControlApi() {
      var group = endpoints.MapGroup( "/api/v1" );

      group.MapGet(
          "/agents",
          ( ControlService service ) => Results.Ok( service.GetAgents() )
        )
        .WithSummary( "List registered agents" );

      group.MapPost(
          "/scans",
          ( StartScanRequest request, ControlService service ) =>
            Results.Accepted( "/api/v1/scans", service.StartScan( request ) )
        )
        .WithSummary( "Start a network scan" );

      group.MapGet(
        "/scans/{id}",
        ( string id, ControlService service ) => {
          try {
            return Results.Ok( service.GetScan( id ) );
          }
          catch ( KeyNotFoundException exception ) {
            return Results.Problem( exception.Message, statusCode: StatusCodes.Status404NotFound );
          }
        }
      );

      group.MapGet(
        "/scans/{id}/results",
        ( string id, ControlService service ) => {
          try {
            return Results.Ok( service.GetResult( id ) );
          }
          catch ( KeyNotFoundException exception ) {
            return Results.Problem( exception.Message, statusCode: StatusCodes.Status404NotFound );
          }
        }
      );

      group.MapGet(
        "/scans/{id}/events",
        async ( string id, ControlService service, HttpResponse response, CancellationToken cancellationToken ) => {
          if ( !service.Exists( id ) ) {
            return Results.Problem( $"Scan '{id}' was not found.", statusCode: StatusCodes.Status404NotFound );
          }

          response.ContentType = "text/event-stream";
          await foreach ( var scanEvent in service.WatchScan( id, cancellationToken ) ) {
            await response.WriteAsync(
              $"event: scan\nid: {scanEvent.ScanId}\ndata: {JsonSerializer.Serialize( scanEvent, ControlJsonSerializerContext.Default.ScanEvent )}\n\n",
              cancellationToken
            );
            await response.Body.FlushAsync( cancellationToken );
          }

          return Results.Empty;
        }
      );
    }
  }
}