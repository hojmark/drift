using System.Text.Json;
using Drift.Coordinator.Api.Control.Dtos;
using Drift.Coordinator.Services;
using Drift.Coordinator.Services.Agents;
using Drift.Coordinator.Services.Models;
using Drift.Coordinator.Services.Scans;
using Drift.Coordinator.Services.Spec;
using Drift.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Drift.Coordinator.Api.Control;

internal static class EndpointRouteBuilderExtensions {
  extension( IEndpointRouteBuilder endpoints ) {
    public void MapControlApi() {
      var api = endpoints.MapGroup( "/api/v1" ).WithTags( "drift" );

      api.MapGet(
          "/status",
          () => Results.Ok( ServerStatusService.GetStatus().ToDto() )
        )
        .WithSummary( "Get server status" )
        .Produces<ServerStatusDto>();

      api.MapPut(
          "/spec",
          async ( HttpRequest request, [FromServices] SpecService service, CancellationToken cancellationToken ) => {
            using var reader = new StreamReader( request.Body );
            var yaml = await reader.ReadToEndAsync( cancellationToken );
            var validation = await service.ReplaceAsync( yaml, cancellationToken );
            if ( validation.IsValid ) {
              return Results.NoContent();
            }

            return Results.ValidationProblem(
              validation.Errors
                .GroupBy( error => string.IsNullOrWhiteSpace( error.Path ) ? "spec" : error.Path )
                .ToDictionary( group => group.Key, group => group.Select( error => error.Message ).ToArray() )
            );
          }
        )
        .WithSummary( "Replace the coordinator spec" )
        .Accepts<string>( "text/plain" )
        .Produces( StatusCodes.Status204NoContent );

      api.MapGet(
          "/spec",
          ( [FromServices] SpecService service ) => Results.Text( service.GetYaml(), "text/yaml" )
        )
        .WithSummary( "Get the coordinator spec" )
        .Produces( StatusCodes.Status200OK, contentType: "text/yaml" );

      api.MapGet(
          "/agents",
          async (
            [FromServices] AgentManagementService service,
            [FromServices] AgentGateway agentGateway,
            CancellationToken cancellationToken
          ) => {
            var agents = service.ListAgents();
            await Task.WhenAll( agents.Select( agent => RefreshAgentStatusAsync(
              agent,
              agentGateway,
              cancellationToken
            ) ) );
            return Results.Ok( service.ListAgents().Select( ToDto ).ToArray() );
          }
        )
        .WithSummary( "List registered agents and refresh their connection status" )
        .Produces<AgentStateDto[]>();

      api.MapPost(
          "/agents/enrollments",
          async (
            [FromBody] EnrollAgentRequest request,
            [FromServices] AgentManagementService service,
            [FromServices] AgentGateway agentGateway,
            [FromServices] ILogger logger,
            CancellationToken cancellationToken
          ) => {
            try {
              logger.LogInformation( "Agent enrollment requested for {AgentId}", request.Id );
              var state = service.EnrollAgent( new EnrollAgentCommand( AgentId.Parse( request.Id, null ) ) );
              await agentGateway.CheckStatusAsync( state.Id, cancellationToken );
              logger.LogInformation( "Agent {AgentId} enrolled and connected", state.Id );
              return Results.Ok( service.ListAgents().Single( agent => agent.Id == state.Id ).ToDto() );
            }
            catch ( ArgumentException exception ) {
              logger.LogWarning( exception, "Agent enrollment rejected for {AgentId}", request.Id );
              return Results.Problem(
                title: "Agent enrollment rejected",
                detail: GetArgumentErrorMessage( exception ),
                statusCode: StatusCodes.Status400BadRequest
              );
            }
            catch ( Exception exception ) {
              logger.LogWarning( exception, "Agent enrollment connection check failed for {AgentId}", request.Id );
              return Results.Problem(
                title: "Agent connection failed",
                detail: $"Unable to connect to agent '{request.Id}': {exception.Message}",
                statusCode: StatusCodes.Status503ServiceUnavailable
              );
            }
          }
        )
        .WithSummary( "Enroll an agent" )
        .Produces<AgentStateDto>()
        .ProducesProblem( StatusCodes.Status400BadRequest )
        .ProducesProblem( StatusCodes.Status503ServiceUnavailable );

      api.MapPost(
          "/agents/enrollment-requests",
          (
            [FromBody] EnrollAgentRequest request,
            [FromServices] AgentManagementService service,
            [FromServices] ILogger logger
          ) => {
            try {
              logger.LogInformation( "Agent enrollment request received for {AgentId}", request.Id );
              return Results.Ok( service.EnrollAgent( new EnrollAgentCommand( AgentId.Parse( request.Id, null ) ) )
                .ToDto() );
            }
            catch ( ArgumentException exception ) {
              logger.LogWarning( exception, "Agent enrollment request rejected for {AgentId}", request.Id );
              return Results.Problem(
                title: "Agent enrollment request rejected",
                detail: GetArgumentErrorMessage( exception ),
                statusCode: StatusCodes.Status400BadRequest
              );
            }
          }
        )
        .WithSummary( "Request agent enrollment" )
        .Produces<AgentStateDto>()
        .ProducesProblem( StatusCodes.Status400BadRequest );

      api.MapDelete(
          "/agents/{id}",
          ( AgentId id, [FromServices] AgentManagementService service ) =>
            service.UnenrollAgent( id )
              ? Results.NoContent()
              : Results.NotFound()
        )
        .WithSummary( "Remove an enrolled agent" )
        .Produces( StatusCodes.Status204NoContent )
        .Produces( StatusCodes.Status404NotFound );

      api.MapPost(
          "/scans",
          ( StartScanRequest request, [FromServices] ScanService service ) => {
            if ( request.PingsPerSecond <= 0 ) {
              return Results.ValidationProblem( new Dictionary<string, string[]> {
                ["pingsPerSecond"] = ["Pings per second must be greater than zero."]
              } );
            }

            return Results.Accepted(
              "/api/v1/scans",
              service.Start( new StartScanCommand( request.Cidrs, (uint) request.PingsPerSecond ) ).ToDto()
            );
          }
        )
        .WithSummary( "Start a network scan" )
        .Produces<StartScanResponseDto>( StatusCodes.Status202Accepted )
        .ProducesValidationProblem();

      api.MapGet(
        "/scans/{id:guid}",
        ( Guid id, [FromServices] ScanService service ) => {
          try {
            return Results.Ok( service.Get( id ).ToDto() );
          }
          catch ( KeyNotFoundException exception ) {
            return Results.Problem( exception.Message, statusCode: StatusCodes.Status404NotFound );
          }
        }
      ).Produces<ScanStatusResponseDto>().ProducesProblem( StatusCodes.Status404NotFound );

      api.MapGet(
          "/scans/{id:guid}/results",
          ( Guid id, [FromServices] ScanService service ) => {
            try {
              return Results.Text(
                service.GetResultJson( id ).GetRawText(),
                contentType: "application/json"
              );
            }
            catch ( KeyNotFoundException exception ) {
              return Results.Problem( exception.Message, statusCode: StatusCodes.Status404NotFound );
            }
          }
        ).Produces( StatusCodes.Status200OK, contentType: "application/json" )
        .ProducesProblem( StatusCodes.Status404NotFound );

      api.MapGet(
          "/scans/{id:guid}/events",
          async (
            Guid id,
            [FromServices] ScanService service,
            HttpRequest request,
            HttpResponse response,
            CancellationToken cancellationToken
          ) => {
            if ( !service.Exists( id ) ) {
              return Results.Problem( $"Scan '{id}' was not found.", statusCode: StatusCodes.Status404NotFound );
            }

            response.ContentType = "text/event-stream";
            await using var eventStream = service.Watch( id, cancellationToken )
              .GetAsyncEnumerator( cancellationToken );
            using var keepAlive = new PeriodicTimer( TimeSpan.FromSeconds( 15 ) );
            var nextEvent = MoveNextAsync( eventStream );
            var nextKeepAlive = WaitForKeepAliveAsync( keepAlive, cancellationToken );
            while ( true ) {
              var completedTask = await Task.WhenAny( nextEvent, nextKeepAlive );
              if ( completedTask == nextKeepAlive ) {
                await response.WriteAsync( ": keepalive\n\n", cancellationToken );
                await response.Body.FlushAsync( cancellationToken );
                nextKeepAlive = WaitForKeepAliveAsync( keepAlive, cancellationToken );
                continue;
              }

              if ( !await nextEvent ) {
                break;
              }

              var scanEvent = eventStream.Current;
              await response.WriteAsync(
                $"event: scan\nid: {scanEvent.EventId}\ndata: {JsonSerializer.Serialize( scanEvent.ToDto(), JsonSerializerContext.Default.ScanEventDto )}\n\n",
                cancellationToken
              );
              await response.Body.FlushAsync( cancellationToken );
              nextEvent = MoveNextAsync( eventStream );
            }

            return Results.Empty;
          }
        ).Produces( StatusCodes.Status200OK, contentType: "text/event-stream" )
        .ProducesProblem( StatusCodes.Status404NotFound );
    }
  }

  private static async Task<bool> MoveNextAsync( IAsyncEnumerator<ScanEventData> eventStream ) {
    return await eventStream.MoveNextAsync();
  }

  private static async Task<bool>
    WaitForKeepAliveAsync( PeriodicTimer keepAlive, CancellationToken cancellationToken ) {
    return await keepAlive.WaitForNextTickAsync( cancellationToken );
  }

  private static async Task RefreshAgentStatusAsync(
    AgentState agent,
    AgentGateway agentGateway,
    CancellationToken cancellationToken
  ) {
    try {
      await agentGateway.CheckStatusAsync(
        agent.Id,
        TimeSpan.FromSeconds( 1 ),
        cancellationToken
      );
    }
    catch ( Exception ) when ( !cancellationToken.IsCancellationRequested ) {
      // AgentGateway records the unavailable state before propagating the request failure.
    }
  }

  private static string GetArgumentErrorMessage( ArgumentException exception ) {
    if ( exception.ParamName is not { } parameterName ) {
      return exception.Message;
    }

    var suffix = $" (Parameter '{parameterName}')";
    return exception.Message.EndsWith( suffix, StringComparison.Ordinal )
      ? exception.Message[..^suffix.Length]
      : exception.Message;
  }

  private static AgentStateDto ToDto( this AgentState state ) => new(
    state.Id.Value,
    state.Address,
    (AgentEnrollmentStatusDto) state.EnrollmentStatus,
    (AgentConnectionStatusDto) state.ConnectionStatus
  );

  private static StartScanResponseDto ToDto( this ScanStartResult result ) => new(
    result.ScanId,
    (ScanStatusDto) result.Status
  );

  private static ScanStatusResponseDto ToDto( this ScanStatusResult result ) => new(
    result.ScanId,
    (ScanStatusDto) result.Status,
    result.Progress
  );

  private static ScanEventDto ToDto( this ScanEventData result ) => new(
    result.EventId,
    result.ScanId,
    (ScanStatusDto) result.Status,
    result.Progress,
    result.Result
  );

  private static ServerStatusDto ToDto( this ServerStatusResult result ) => new((ServerStatusDtoValue) result.Status);
}