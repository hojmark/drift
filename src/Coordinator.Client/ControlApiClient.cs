using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Drift.Coordinator.Client.Generated;
using Drift.Coordinator.Client.Models;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Spec.Serialization;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using GeneratedEnrollAgentRequest = Drift.Coordinator.Client.Generated.Models.EnrollAgentRequest;
using GeneratedStartScanRequest = Drift.Coordinator.Client.Generated.Models.StartScanRequest;

namespace Drift.Coordinator.Client;

// Kiota generates nullable response types even when the API contract guarantees a response body.
// This is an unfortunate limitation affecting several calls in this client; see https://github.com/microsoft/kiota/issues/3911.

/// <summary>
/// The Control API client.
/// </summary>
public sealed class ControlApiClient : IDisposable {
  private readonly ControlClient _generatedClient;

  // Used for handwritten SSE which Kiota does not support.
#pragma warning disable S1133
  [Obsolete( "Use the generated client" )]
#pragma warning restore S1133
  private readonly HttpClient _httpClient;

  private ControlApiClient( HttpClient httpClient, ControlClient generatedClient ) {
    // Keep as long as _httpClient is in use
#pragma warning disable CS0618
    _httpClient = httpClient;
#pragma warning restore CS0618
    _generatedClient = generatedClient;
  }

  public static ControlApiClient Create( Uri address ) {
    var httpClient = new HttpClient( new ProblemDetailsHandler( new HttpClientHandler() ) ) {
      BaseAddress = NormalizeAddress( address )
    };
    var requestAdapter = new HttpClientRequestAdapter( new AnonymousAuthenticationProvider(), httpClient: httpClient ) {
      BaseUrl = httpClient.BaseAddress.ToString().TrimEnd( '/' )
    };
    return new ControlApiClient( httpClient, new ControlClient( requestAdapter ) );
  }

  /// <summary>
  /// Replaces the coordinator's persisted specification.
  /// </summary>
  public async Task ApplySpecAsync( string yaml, CancellationToken cancellationToken ) {
    try {
      await _generatedClient.Api.V1.Spec.PutAsync( yaml, cancellationToken: cancellationToken );
    }
    catch ( ControlApiHttpException exception ) {
      throw CreateRequestException(
        exception.StatusCode ?? HttpStatusCode.InternalServerError,
        exception.ReasonPhrase,
        exception.Body,
        exception
      );
    }
    catch ( ApiException exception ) {
      throw CreateRequestException( exception );
    }
  }

  /// <summary>
  /// Gets the coordinator's persisted specification.
  /// </summary>
  public async Task<Network> GetSpecAsync( CancellationToken cancellationToken ) {
    try {
      await using var stream = await _generatedClient.Api.V1.Spec.GetAsync(
        cancellationToken: cancellationToken
      ) ?? throw new InvalidOperationException( "The coordinator returned an empty spec response." );
      return YamlConverter.Deserialize( stream ).Network;
    }
    catch ( ControlApiHttpException exception ) {
      throw CreateRequestException(
        exception.StatusCode ?? HttpStatusCode.InternalServerError,
        exception.ReasonPhrase,
        exception.Body,
        exception
      );
    }
    catch ( ApiException exception ) {
      throw CreateRequestException( exception );
    }
  }

  /// <summary>
  /// Enrolls a declared agent and verifies its current connection status.
  /// </summary>
  public async Task<AgentEnrollmentResult> EnrollAgentAsync( AgentId id, CancellationToken cancellationToken ) {
    try {
      var response = await _generatedClient.Api.V1.Agents.Enrollments.PostAsync(
        new GeneratedEnrollAgentRequest { Id = id.Value },
        cancellationToken: cancellationToken
      ) ?? throw new InvalidOperationException( "The coordinator returned an empty enrollment response." );

      return new AgentEnrollmentResult(
        AgentId.Parse(
          response.Id ??
          throw new InvalidOperationException( "The coordinator returned an enrollment without an ID." ),
          null
        ),
        new Uri(
          response.Address ??
          throw new InvalidOperationException( "The coordinator returned an enrollment without an address." )
        ),
        ParseEnrollmentStatus( response.EnrollmentStatus ),
        ParseConnectionStatus( response.ConnectionStatus )
      );
    }
    catch ( ControlApiHttpException exception ) {
      throw CreateRequestException(
        exception.StatusCode ?? HttpStatusCode.InternalServerError,
        exception.ReasonPhrase,
        exception.Body,
        exception
      );
    }
    catch ( ApiException exception ) {
      throw CreateRequestException( exception );
    }
  }

  /// <summary>
  /// Gets the coordinator's current service status.
  /// </summary>
  public async Task<CoordinatorStatus> GetStatusAsync( CancellationToken cancellationToken ) {
    try {
      var response = await _generatedClient.Api.V1.Status.GetAsync( cancellationToken: cancellationToken )
                     ?? throw new InvalidOperationException( "The coordinator returned an empty status response." );

      return new CoordinatorStatus( response.Status switch {
        Generated.Models.ServerStatusDtoValue.Ready => CoordinatorServiceStatus.Ready,
        _ => throw new InvalidOperationException( "The coordinator returned an unknown service status." )
      } );
    }
    catch ( ControlApiHttpException exception ) {
      throw CreateRequestException(
        exception.StatusCode ?? HttpStatusCode.InternalServerError,
        exception.ReasonPhrase,
        exception.Body,
        exception
      );
    }
    catch ( ApiException exception ) {
      throw CreateRequestException( exception );
    }
  }

  /// <summary>
  /// Lists agents currently enrolled with the coordinator.
  /// </summary>
  public async Task<CoordinatorAgentStatus[]> GetAgentsAsync( CancellationToken cancellationToken ) {
    try {
      var responses = await _generatedClient.Api.V1.Agents.GetAsync( cancellationToken: cancellationToken )
                      ?? throw new InvalidOperationException( "The coordinator returned an empty agents response." );
      return responses.Select( response => new CoordinatorAgentStatus(
        AgentId.Parse(
          response.Id ?? throw new InvalidOperationException( "The coordinator returned an agent without an ID." ),
          null
        ),
        new Uri(
          response.Address ??
          throw new InvalidOperationException( "The coordinator returned an agent without an address." )
        ),
        ParseEnrollmentStatus( response.EnrollmentStatus ),
        ParseConnectionStatus( response.ConnectionStatus )
      ) ).ToArray();
    }
    catch ( ControlApiHttpException exception ) {
      throw CreateRequestException(
        exception.StatusCode ?? HttpStatusCode.InternalServerError,
        exception.ReasonPhrase,
        exception.Body,
        exception
      );
    }
    catch ( ApiException exception ) {
      throw CreateRequestException( exception );
    }
  }

  /// <summary>
  /// Starts a coordinator scan and returns its server-side identifier.
  /// </summary>
  public async Task<Guid> StartScanAsync( uint pingsPerSecond, CancellationToken cancellationToken ) {
    var request = new GeneratedStartScanRequest { PingsPerSecond = checked((int) pingsPerSecond) };
    try {
      var response = await _generatedClient.Api.V1.Scans.PostAsync(
        request,
        cancellationToken: cancellationToken
      ) ?? throw new InvalidOperationException( "The coordinator returned an empty scan response." );

      _ = response.Status ??
          throw new InvalidOperationException( "The coordinator returned a scan response without a status." );

      return response.ScanId ??
             throw new InvalidOperationException( "The coordinator returned a scan response without an ID." );
    }
    catch ( ControlApiHttpException exception ) {
      throw CreateRequestException(
        exception.StatusCode ?? HttpStatusCode.InternalServerError,
        exception.ReasonPhrase,
        exception.Body,
        exception
      );
    }
    catch ( ApiException exception ) {
      throw CreateRequestException( exception );
    }
  }

  /// <summary>
  /// Streams scan snapshots from the coordinator using the SSE endpoint.
  /// </summary>
  public async IAsyncEnumerable<NetworkScanResult> WatchScanAsync(
    Guid scanId,
    [EnumeratorCancellation] CancellationToken cancellationToken
  ) {
    // TODO: Add reconnect and resume support for interrupted SSE connections.
    await foreach ( var result in WatchScanConnectionAsync( scanId, cancellationToken ) ) {
      yield return result;
    }
  }

  // Kiota does not currently support consuming Server-Sent Events; keep this transport handwritten
  // until generated SSE support is available: https://github.com/microsoft/kiota/issues/7435.
  private async IAsyncEnumerable<NetworkScanResult> WatchScanConnectionAsync(
    Guid scanId,
    [EnumeratorCancellation] CancellationToken cancellationToken ) {
    using var request = new HttpRequestMessage( HttpMethod.Get, $"api/v1/scans/{scanId}/events" );
    request.Headers.Accept.Add( new MediaTypeWithQualityHeaderValue( "text/event-stream" ) );

#pragma warning disable CS0618
    using var response = await _httpClient.SendAsync(
#pragma warning restore CS0618
      request,
      HttpCompletionOption.ResponseHeadersRead,
      cancellationToken
    );
    await EnsureSuccessAsync( response, cancellationToken );
    await using var stream = await response.Content.ReadAsStreamAsync( cancellationToken );
    using var reader = new StreamReader( stream );

    var data = new StringBuilder();
    var terminalEventReceived = false;
    while ( await reader.ReadLineAsync( cancellationToken ) is { } line ) {
      if ( line.Length > 0 ) {
        if ( line.StartsWith( "data:", StringComparison.Ordinal ) ) {
          if ( data.Length > 0 ) {
            data.Append( '\n' );
          }

          data.Append( line[5..].TrimStart() );
        }

        continue;
      }

      if ( data.Length == 0 ) {
        continue;
      }

      var eventData = JsonSerializer.Deserialize(
        data.ToString(),
        ControlJsonSerializerContext.Default.ScanEventResponse
      ) ?? throw new InvalidOperationException( "The coordinator returned an invalid scan event." );
      data.Clear();
      terminalEventReceived = eventData.Status is
        CoordinatorScanStatus.Completed or
        CoordinatorScanStatus.Failed or
        CoordinatorScanStatus.Cancelled;

      var result = eventData.Result ?? new NetworkScanResult {
        Metadata = new Metadata { StartedAt = DateTime.UtcNow },
        Status = eventData.Status switch {
          CoordinatorScanStatus.Completed => ScanResultStatus.Success,
          CoordinatorScanStatus.Failed => ScanResultStatus.Error,
          CoordinatorScanStatus.Cancelled => ScanResultStatus.Canceled,
          _ => ScanResultStatus.InProgress
        },
        Progress = new Percentage( eventData.Progress )
      };

      yield return result;
    }

    if ( !terminalEventReceived ) {
      throw new IOException( "The coordinator scan event stream ended before the scan completed." );
    }
  }

  /// <summary>
  /// Retrieves a completed scan result after an interrupted event stream.
  /// </summary>
  public async Task<NetworkScanResult> GetResultAsync( Guid scanId, CancellationToken cancellationToken ) {
    try {
      await using var responseStream = await _generatedClient.Api.V1.Scans[scanId].Results.GetAsync(
        cancellationToken: cancellationToken
      ) ?? throw new InvalidOperationException( "The coordinator returned an empty scan result." );

      return await JsonSerializer.DeserializeAsync(
        responseStream,
        ControlJsonSerializerContext.Default.NetworkScanResult,
        cancellationToken
      ) ?? throw new InvalidOperationException( "The coordinator returned an invalid scan result." );
    }
    catch ( ControlApiHttpException exception ) {
      throw CreateRequestException(
        exception.StatusCode ?? HttpStatusCode.InternalServerError,
        exception.ReasonPhrase,
        exception.Body,
        exception
      );
    }
    catch ( ApiException exception ) {
      throw CreateRequestException( exception );
    }
  }

  public void Dispose() {
#pragma warning disable CS0618
    _httpClient.Dispose();
#pragma warning restore CS0618
  }

  private static AgentEnrollmentStatus ParseEnrollmentStatus(
    Generated.Models.AgentEnrollmentStatusDto? status ) => status switch {
    Generated.Models.AgentEnrollmentStatusDto.NotEnrolled => AgentEnrollmentStatus.NotEnrolled,
    Generated.Models.AgentEnrollmentStatusDto.Enrolled => AgentEnrollmentStatus.Enrolled,
    _ => throw new InvalidOperationException( $"The coordinator returned an unknown enrollment status '{status}'." )
  };

  private static AgentConnectionStatus ParseConnectionStatus(
    Generated.Models.AgentConnectionStatusDto? status ) => status switch {
    Generated.Models.AgentConnectionStatusDto.Unknown => AgentConnectionStatus.Unknown,
    Generated.Models.AgentConnectionStatusDto.Connected => AgentConnectionStatus.Connected,
    Generated.Models.AgentConnectionStatusDto.Unavailable => AgentConnectionStatus.Unavailable,
    _ => throw new InvalidOperationException( $"The coordinator returned an unknown connection status '{status}'." )
  };

  private static HttpRequestException CreateRequestException(
    HttpStatusCode statusCode,
    string? reasonPhrase,
    string body,
    Exception innerException
  ) {
    var message = FormatProblemDetails( statusCode, reasonPhrase, body );
    return new HttpRequestException( message, innerException, statusCode );
  }

  private static HttpRequestException CreateRequestException( ApiException exception ) {
    var statusCode = exception.ResponseStatusCode is { } value
      ? (HttpStatusCode) value
      : HttpStatusCode.InternalServerError;

    return CreateRequestException( statusCode, null, exception.Message, exception );
  }

  private static async Task EnsureSuccessAsync( HttpResponseMessage response, CancellationToken cancellationToken ) {
    if ( response.IsSuccessStatusCode ) {
      return;
    }

    var details = await response.Content.ReadAsStringAsync( cancellationToken );
    var message = FormatProblemDetails( response.StatusCode, response.ReasonPhrase, details );

    throw new HttpRequestException( message, inner: null, response.StatusCode );
  }

  private static string FormatProblemDetails( HttpStatusCode statusCode, string? reasonPhrase, string body ) {
    var prefix = $"Coordinator request failed with {(int) statusCode} ({reasonPhrase ?? statusCode.ToString()}).";
    if ( string.IsNullOrWhiteSpace( body ) ) {
      return prefix;
    }

    try {
      var problem = JsonSerializer.Deserialize( body, ControlJsonSerializerContext.Default.ProblemDetailsResponse );
      var validationMessages = problem?.Errors?
        .Where( error => error.Value.ValueKind == JsonValueKind.Array )
        .SelectMany( error => error.Value.EnumerateArray()
          .Where( message => message.ValueKind == JsonValueKind.String )
          .Select( message => $"{error.Key}: {message.GetString()}" ) )
        .ToList() ?? [];

      var message = problem?.Title is not null && problem.Detail is not null
        ? $"{problem.Title}: {problem.Detail}"
        : problem?.Title ?? problem?.Detail;
      if ( validationMessages.Count > 0 ) {
        var validationMessage = string.Join( " ", validationMessages.Distinct( StringComparer.Ordinal ) );
        message = message is null ? validationMessage : $"{message}: {validationMessage}";
      }

      return string.IsNullOrWhiteSpace( message )
        ? $"{prefix} Server returned no additional details."
        : $"{prefix} {message}";
    }
    catch ( JsonException ) {
      return $"{prefix} {body}";
    }
  }

  private static Uri NormalizeAddress( Uri address ) {
    if ( address.IsAbsoluteUri ) {
      return address;
    }

#pragma warning disable S5332
    return new Uri( $"http://{address}" );
#pragma warning restore S5332
  }

  private sealed class ProblemDetailsHandler( HttpMessageHandler innerHandler ) : DelegatingHandler( innerHandler ) {
    protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken ) {
      var response = await base.SendAsync( request, cancellationToken );
      if ( response.IsSuccessStatusCode ) {
        return response;
      }

      var body = await response.Content.ReadAsStringAsync( cancellationToken );
      var statusCode = response.StatusCode;
      var reasonPhrase = response.ReasonPhrase;
      response.Dispose();
      throw new ControlApiHttpException( statusCode, reasonPhrase, body );
    }
  }

  private sealed class ControlApiHttpException(
    HttpStatusCode statusCode,
    string? reasonPhrase,
    string body
  ) : HttpRequestException( FormatProblemDetails( statusCode, reasonPhrase, body ), inner: null, statusCode ) {
    public string? ReasonPhrase {
      get;
    } = reasonPhrase;

    public string Body {
      get;
    } = body;
  }
}

internal sealed record ProblemDetailsResponse(
  string? Title,
  string? Detail,
  Dictionary<string, JsonElement>? Errors
);

public sealed record AgentEnrollmentResult(
  AgentId Id,
  Uri Address,
  AgentEnrollmentStatus EnrollmentStatus,
  AgentConnectionStatus ConnectionStatus
);

public sealed record ScanStartResponse( Guid ScanId, CoordinatorScanStatus Status );

public sealed record ScanEventResponse(
  long EventId,
  Guid ScanId,
  CoordinatorScanStatus Status,
  byte Progress,
  NetworkScanResult? Result
);