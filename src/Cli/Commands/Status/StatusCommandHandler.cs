using System.Net.NetworkInformation;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Settings.Serialization;
using Drift.Cli.Settings.V1_preview;
using Drift.Cli.Settings.V1_preview.Environments;
using Drift.Common.IO;
using Drift.Coordinator.Client;
using Drift.Coordinator.Client.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Drift.Cli.Commands.Status;

internal sealed class StatusCommandHandler(
  IOutputManager output,
  ISettingsLocation settingsLocation,
  IDriftDataLocation driftDataLocation
) : ICommandHandler<StatusParameters> {
  private static readonly TimeSpan StatusProbeTimeout = TimeSpan.FromSeconds( 2 );

  public async Task<int> Invoke( StatusParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'status' command" );

    var settings = CliSettings.Read( settingsLocation, output.GetLogger() );
    var activeEnvironment = string.IsNullOrWhiteSpace( settings.ActiveEnvironment )
      ? BuiltInEnvironmentNames.Local
      : settings.ActiveEnvironment;

    var environments = new List<StatusEnvironment>();
    if ( string.Equals( activeEnvironment, BuiltInEnvironmentNames.Local, StringComparison.OrdinalIgnoreCase ) ) {
      environments.Add(
        new StatusEnvironment(
          BuiltInEnvironmentNames.Local,
          "local",
          true,
          true,
          "Local scanning",
          [],
          null
        )
      );
    }
    else if ( settings.Environments.SingleOrDefault( environment =>
               string.Equals( environment.Name, activeEnvironment, StringComparison.OrdinalIgnoreCase )
             ) is { } activeServer ) {
      environments.Add( await GetServerStatusAsync( activeServer, true, cancellationToken ) );
    }
    else {
      environments.Add(
        new StatusEnvironment(
          activeEnvironment,
          "unknown",
          true,
          false,
          "Unavailable",
          [],
          $"Active environment '{activeEnvironment}' does not exist."
        )
      );
    }

    Render( environments, settingsLocation, driftDataLocation );
    return ExitCodes.Success;
  }

  private static async Task<StatusEnvironment> GetServerStatusAsync(
    EnvironmentSetting environment,
    bool isActive,
    CancellationToken cancellationToken
  ) {
    Uri address;
    try {
      address = EnvironmentTargetProvider.NormalizeAddress( environment.Address );
    }
    catch ( FormatException exception ) {
      return new StatusEnvironment(
        environment.Name,
        environment.Address,
        isActive,
        false,
        "Invalid address",
        [],
        exception.Message
      );
    }

    using var probeCancellation = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );
    probeCancellation.CancelAfter( StatusProbeTimeout );

    try {
      using var client = ControlApiClient.Create( address );
      var statusTask = client.GetStatusAsync( probeCancellation.Token );
      var agentsTask = client.GetAgentsAsync( probeCancellation.Token );
      await Task.WhenAll( statusTask, agentsTask );

      return new StatusEnvironment(
        environment.Name,
        environment.Address,
        isActive,
        false,
        ( await statusTask ).Status.ToString(),
        await agentsTask,
        null
      );
    }
    catch ( OperationCanceledException ) when ( !cancellationToken.IsCancellationRequested ) {
      return new StatusEnvironment(
        environment.Name,
        environment.Address,
        isActive,
        false,
        "Unavailable",
        [],
        $"Status probe timed out after {StatusProbeTimeout.TotalSeconds:0.#} seconds."
      );
    }
    catch ( Exception exception ) when ( IsStatusFailure( exception, cancellationToken ) ) {
      return new StatusEnvironment(
        environment.Name,
        environment.Address,
        isActive,
        false,
        "Unavailable",
        [],
        exception.Message
      );
    }
  }

  private void Render(
    IReadOnlyCollection<StatusEnvironment> environments,
    ISettingsLocation settingsLocation,
    IDriftDataLocation driftDataLocation
  ) {
    var console = output.Normal.GetAnsiConsole();

    console.MarkupLine( "[bold]Environment[/]" );
    console.MarkupLine( "-------------------------------" );

    foreach ( var environment in environments ) {
      console.MarkupLine( $"  {Escape( environment.Name )} [grey]@ {Escape( environment.Address )}[/]" );
      console.MarkupLine( $"  Status: {FormatStatus( environment.State )}" );

      if ( environment.Error is not null ) {
        console.MarkupLine( $"          [red]{Escape( environment.Error )}[/]" );
        continue;
      }

      if ( environment.IsLocal ) {
        continue;
      }

      console.MarkupLine(
        environment.Agents.Count == 0
          ? "  Agents: none"
          : $"  Agents ({environment.Agents.Count}):"
      );

      foreach ( var agent in environment.Agents ) {
        console.MarkupLine(
          $"    {Escape( agent.Id )} [grey]@ {Escape( agent.Address.ToString() )}[/] — {FormatStatus( agent.ConnectionStatus )}"
        );
      }
    }

    console.WriteLine();

    console.MarkupLine( "[bold]CLI[/]" );
    console.MarkupLine( "-------------------------------" );

    console.MarkupLine( $"  Data directory: {Escape( driftDataLocation.Directory )}" );
    console.MarkupLine( $"  Config directory: {Escape( settingsLocation.Directory )}" );
    RenderLocalAddresses( console );
  }

  private static string FormatStatus( string status ) {
    var escapedStatus = Escape( status );
    return status switch {
      "Connected" or "Ready" => $"[green]{escapedStatus}[/]",
      "Unavailable" or "Failed" => $"[red]{escapedStatus}[/]",
      "Local scanning" => $"[cyan]{escapedStatus}[/]",
      _ => $"[yellow]{escapedStatus}[/]"
    };
  }

  private static string FormatStatus( AgentConnectionStatus status ) => FormatStatus( status.ToString() );

  private static void RenderLocalAddresses( IAnsiConsole console ) {
    console.MarkupLine( "  Local addresses:" );

    var interfaces = NetworkInterface.GetAllNetworkInterfaces()
      .Where( networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up )
      .SelectMany( networkInterface => networkInterface.GetIPProperties().UnicastAddresses.Select( address =>
          ( Interface: networkInterface.Name, Address: address.Address.ToString() )
        )
      )
      .OrderBy( address => address.Interface, StringComparer.Ordinal )
      .ThenBy( address => address.Address, StringComparer.Ordinal )
      .ToArray();

    if ( interfaces.Length == 0 ) {
      console.MarkupLine( "    none" );
      return;
    }

    foreach ( var (interfaceName, address) in interfaces ) {
      console.MarkupLine( $"    [cyan]{Escape( interfaceName )}[/]: {Escape( address )}" );
    }
  }

  private static string Escape( string value ) => Markup.Escape( value );

  private static bool IsStatusFailure( Exception exception, CancellationToken cancellationToken ) {
    return !cancellationToken.IsCancellationRequested && exception is
      HttpRequestException or IOException or InvalidOperationException or TaskCanceledException;
  }
}