using System.Net.NetworkInformation;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Settings.V1_preview;
using Drift.Cli.Settings.V1_preview.Environments;
using Drift.Cli.SpecFile;
using Drift.Common.IO;
using Drift.Coordinator.Client;
using Drift.Coordinator.Client.Models;
using Drift.Spec.Serialization;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Drift.Cli.Commands.Status;

internal sealed class StatusCommandHandler(
  IOutputManager output,
  IDriftSettingsLocation settingsLocation,
  IDriftDataLocation dataLocation
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
      var (localSpecFile, specSummary) = GetLocalSpecStatus();
      environments.Add(
        new StatusEnvironment(
          BuiltInEnvironmentNames.Local,
          "local",
          true,
          true,
          "Local scanning",
          [],
          null,
          specSummary,
          localSpecFile
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

    Render( environments );

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
      var specTask = client.GetSpecAsync( probeCancellation.Token );
      await Task.WhenAll( statusTask, agentsTask, specTask );
      var spec = await specTask;
      var specSummary = spec is null
        ? "none"
        : $"uploaded — {FormatCount( spec.Subnets.Count, "subnet" )}, {FormatCount( spec.Devices.Count, "device" )}";
      var state = ( await statusTask ).Status switch {
        CoordinatorServiceStatus.SpecMissing => "Spec missing",
        var status => status.ToString()
      };

      return new StatusEnvironment(
        environment.Name,
        environment.Address,
        isActive,
        false,
        state,
        await agentsTask,
        null,
        specSummary
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

  private void Render( IReadOnlyCollection<StatusEnvironment> environments ) {
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

      RenderSpecStatus( console, environment );

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

    RenderDirectory( console, "Data directory", dataLocation.Directory );
    RenderDirectory( console, "Settings directory", settingsLocation.Directory );
    RenderLocalAddresses( console );
  }

  /// <summary>
  /// Renders the directory as a file:// hyperlink (OSC 8).
  /// </summary>
  private static void RenderDirectory( IAnsiConsole console, string label, string directory ) {
    var line = new Paragraph( $"  {label}: " );
    line.Append( directory, style: null, link: new Link( new Uri( directory ).AbsoluteUri ) );
    console.Write( line );
    console.WriteLine();
  }

  private static void RenderSpecStatus( IAnsiConsole console, StatusEnvironment environment ) {
    if ( environment.SpecSummary == "Not uploaded" ) {
      console.MarkupLine( "  Spec: [yellow]not uploaded[/]" );
      return;
    }

    if ( environment.SpecSummary.StartsWith( "Uploaded", StringComparison.Ordinal ) ) {
      var details = environment.SpecSummary["Uploaded".Length..];
      console.MarkupLine( $"  Spec: uploaded{Escape( details )}" );
      return;
    }

    var line = new Paragraph( "  Spec: " );
    if ( environment.LocalSpecFile is { } specFile ) {
      line.Append(
        specFile.FullName,
        style: null,
        link: new Link( new Uri( specFile.FullName ).AbsoluteUri )
      );
      line.Append( $" — {environment.SpecSummary}" );
    }
    else {
      line.Append( environment.SpecSummary );
    }

    console.Write( line );
    console.WriteLine();
  }

  private (FileInfo? SpecFile, string Summary) GetLocalSpecStatus() {
    var specFile = new SpecFilePathResolver( output, Directory.GetCurrentDirectory() ).Resolve( null );
    if ( specFile is null ) {
      return ( null, "Not found" );
    }

    try {
      var network = YamlConverter.Deserialize( File.ReadAllText( specFile.FullName ) ).Network;
      return (
        specFile,
        $"{FormatCount( network.Subnets.Count, "subnet" )}, {FormatCount( network.Devices.Count, "device" )}"
      );
    }
    catch ( Exception exception ) {
      output.Log.LogDebug( exception, "Could not read local spec details from {SpecPath}", specFile.FullName );
      return ( specFile, "details unavailable" );
    }
  }

  private static string FormatCount( int count, string resource ) =>
    $"{count} {resource}{( count == 1 ? string.Empty : "s" )}";

  private static string FormatStatus( string status ) {
    var escapedStatus = Escape( status.ToLowerInvariant() );
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