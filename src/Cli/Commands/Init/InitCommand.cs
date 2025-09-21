using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Scan;
using Drift.Cli.Output;
using Drift.Cli.Output.Abstractions;
using Drift.Cli.Output.Logging;
using Drift.Cli.Output.Normal;
using Drift.Core.Scan.Subnets.Interface;
using Drift.Diff.Domain;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Domain.Extensions;
using Drift.Domain.Scan;
using Drift.Utils;
using Microsoft.Extensions.Logging;
using NaturalSort.Extension;
using Spectre.Console;
using Environment = System.Environment;

namespace Drift.Cli.Commands.Init;

internal class InitCommand : CommandBase<InitParameters, InitCommandHandler> {
  /// Intended for testing (although I should maybe look into a better way to do this e.g., Linux 'expect')
  internal static readonly Option<ForceMode?> ForceModeOption = new("--force-mode") {
    Description = "(HIDDEN) Force mode", Arity = ArgumentArity.ZeroOrOne, Hidden = true
  };

  // Should perform the same default scan as the scan command to give a good first impression
  // TODO structure code so this always happens
  internal static readonly Option<bool?> DiscoverOption = new("--discover") {
    Description = "Populate with devices and subnets discovered in a network scan", Arity = ArgumentArity.ZeroOrOne
  };

  internal static readonly Option<bool?> OverwriteOption = new("--overwrite") {
    Description = "Overwrite existing file", Arity = ArgumentArity.ZeroOrOne
  };

  internal InitCommand( IServiceProvider provider ) : base(
    "init",
    "Create a network spec",
    provider
  ) {
    Add( ForceModeOption );
    Add( DiscoverOption );
    Add( OverwriteOption );

    /*var withEnvOption = new Option<bool>(
      "--with-env",
      "Also create an environment config alongside the spec"
    ) { Arity = ArgumentArity.ZeroOrOne };*/

    //TODO support examples
    /*initCommand.WithExamples(
      "drift init     (interactive)",
      "drift init main-site",
      "drift init main-site --discover --with-env"
    );*/
  }

  protected override InitParameters CreateParameters( ParseResult result ) {
    return new InitParameters( result );
  }
}

internal class InitCommandHandler(
  IOutputManager output,
  INetworkScanner scanner,
  IInterfaceSubnetProvider interfaceSubnetProvider
) : ICommandHandler<InitParameters> {
  public async Task<int> Invoke( InitParameters parameters, CancellationToken cancellationToken ) {
    var isInteractive = IsInteractiveMode(
      parameters.ForceMode,
      parameters.SpecFile,
      parameters.Overwrite,
      parameters.Discover
    );

    if ( isInteractive && parameters.OutputFormat != OutputFormat.Normal ) {
      throw new ArgumentException( "Interactive mode is not supported with non-normal output format" );
    }

    output.Log.LogDebug( "Running init command" );
    // TODO skip emojis in output if redirected?

    var initOptions = isInteractive
      ? RunInteractive( output.Normal )
      : RunNonInteractive( output, parameters.SpecFile?.Name, parameters.Overwrite, parameters.Discover );

    if ( initOptions == null ) {
      return ExitCodes.GeneralError;
    }

    var success = await Initialize( initOptions );

    if ( !success ) {
      return ExitCodes.GeneralError;
    }

    if ( success && isInteractive ) {
      output.Normal.WriteLine();
      output.Normal.WriteLineCTA( "💡\uFE0F Next: Try", $"drift scan {initOptions.Name}" );
    }

    output.Log.LogDebug( "init command completed" );

    return ExitCodes.Success;
  }

  // Detects if all inputs are unset or empty — if so, assume interactive mode.
  // Intended for optional string and bool? args.
  private static bool IsInteractiveMode( ForceMode? forceMode, params object?[] options ) {
    if ( forceMode.HasValue ) {
      return forceMode.Value == ForceMode.Interactive;
    }

    return !Console.IsInputRedirected &&
           options.All( opt =>
             opt is null ||
             ( opt is string s && string.IsNullOrWhiteSpace( s ) )
           );
  }

  private static InitOptions RunInteractive( INormalOutput console ) {
    var driftStatePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".drift" );

    console.WriteLine();

    console.GetAnsiConsole().MarkupLine( "[bold]📡\uFE0F Welcome to Drift! Let's set up a new spec.[/]" );

    console.WriteLine();

    var name = console.PromptString( "🌐\uFE0F What should your network be called?", "main-site" );

    //var withEnv = PromptBool( "🗃\uFE0F  Create environment file too?", defaultOption: PromptOption.Yes );

    var discover = console.PromptBool(
      "🔍\uFE0F Run discovery scan to pre-fill with devices and subnets?",
      defaultOption: NormalOutputExtensions.PromptOption.Yes
    );

    var specPath = Path.Combine( ".", $"{name}.spec.yaml" );
    var envPath = Path.Combine( ".", $"{name}.env.yaml" );

    var overwrite = false;

    if ( File.Exists( specPath ) || File.Exists( envPath ) ) {
      overwrite = console.PromptBool(
        "⚠️\uFE0F  File already exist. Overwrite?", // TODO pluralize when supporting env files too
        defaultOption: NormalOutputExtensions.PromptOption.No
      );
    }

    console.WriteLine();

    return new InitOptions { Name = name, Overwrite = overwrite, Discover = discover };
  }

  private static InitOptions?
    RunNonInteractive( IOutputManager output, string? name, bool? overwrite, bool? discover ) {
    if ( name == null ) {
      output.Normal.WriteError( $"{Chars.Cross} Name is required" );
      output.Log.LogError( "Name is required" );
      return null;
    }

    return new InitOptions { Name = name, Overwrite = overwrite ?? false, Discover = discover ?? false };
  }

  private async Task<bool> Initialize( InitOptions options ) {
    try {
      var specPath = Path.GetFullPath( Path.Combine( ".", $"{options.Name}.spec.yaml" ) );
      var envPath = Path.GetFullPath( Path.Combine( ".", $"{options.Name}.env.yaml" ) );

      if ( File.Exists( specPath ) ) {
        switch ( options.Overwrite ) {
          case true:
            output.Normal.WriteLineVerbose( $"Spec file already exists: {specPath} (overwriting)" );
            output.Log.LogDebug( "Spec file already exists: {SpecPath} (overwriting)", specPath );
            break;
          case false:
            output.Normal.WriteError( $"{Chars.Cross} Spec file already exists: " );
            output.Normal.WriteLineError( TextHelper.Bold( $"{specPath}" ) );
            output.Log.LogError( "Spec file already exists: {SpecPath}", specPath );
            return false;
        }
      }

      // SCAN
      var subnets = interfaceSubnetProvider.Get().ToList();

      var scanRequest = new NetworkScanOptions { Cidrs = subnets };

      NetworkScanResult? scanResult = null;

      //TODO create unit test for this
      output.Normal.WriteLineVerbose(
        "Found subnets: " + string.Join( ", ",
          subnets.Select( cidr =>
            cidr + " (" + IpNetworkUtils.GetIpRangeCount( cidr ) +
            " addresses, " +
            scanRequest.EstimatedDuration( cidr ) /* TODO .Humanize( 2, CultureInfo.InvariantCulture )*/ +
            " estimated scan time" +
            ")"
          )
        )
      );

      if ( options.Discover ) {
        if ( output.Is( OutputFormat.Normal ) ) {
          await AnsiConsole
            .Status()
            .StartAsync( "Scanning network ...", async ctx => {
              scanResult = await scanner.ScanAsync( scanRequest, output.GetLogger() );
              await Task.Delay( 1500 );
            } );
        }

        if ( output.Is( OutputFormat.Log ) ) {
          output.Log.LogInformation( "Scanning network..." );
          var lastLogTime = DateTime.MinValue;

          EventHandler<NetworkScanResult> updater = ( _, r ) => {
            ScanCommandHandler.UpdateProgressLog( r.Progress, output, ref lastLogTime );
          };

          scanner.ResultUpdated += updater;

          try {
            scanResult = await scanner.ScanAsync( scanRequest, cancellationToken: CancellationToken.None );
          }
          finally {
            scanner.ResultUpdated -= updater;
          }
        }

        if ( scanResult == null ) {
          output.Log.LogDebug( "Scan result is null" );
          output.Normal.WriteLineError( "Scan result is null" );
          return false;
        }

        output.Log.LogInformation( "Scan completed" );
        output.Log.LogDebug( "Found {Count} devices",
          scanResult.Subnets.Select( subnet => subnet.DiscoveredDevices.Count() ).Sum()
        );

        output.Log.LogInformation( "Writing spec..." );

        CreateSpecWithDiscovery( scanResult, subnets, specPath );
      }
      else {
        output.Log.LogDebug( "No discovery, writing template spec" );
        output.Normal.WriteLineVerbose( "No discovery, writing template spec" );

        output.Log.LogInformation( "Writing spec..." );

        CreateSpecWithoutDiscovery( specPath );
      }

      var fullPath = Path.GetFullPath( specPath );

      if ( output.Is( OutputFormat.Normal ) ) {
        output.Normal.Write( $"{Chars.Checkmark}", ConsoleColor.Green );
        output.Normal.Write( "  Created spec " );
        output.Normal.WriteLine( TextHelper.Bold( $"{fullPath}" ) );
      }

      if ( output.Is( OutputFormat.Log ) ) {
        output.Log.LogInformation( "Created spec: {SpecPath}", specPath );
      }

      return true;
    }
    catch ( Exception e ) {
      //TODO create generic catch for all commands
      output.Normal.WriteLineError( "Unexpected error" );
      output.Normal.WriteLineError( e.ToString() );
      output.Log.LogError( e, "Unexpected error" );
      throw;
    }
  }

  internal static void CreateSpecWithDiscovery(
    NetworkScanResult? scanResult,
    List<CidrBlock> subnets,
    string specPath
  ) {
    var devices = scanResult?.Subnets.SelectMany( subnet => subnet.DiscoveredDevices ).ToDeclared() ?? [];
    CreateSpec( subnets, devices, specPath );
  }

  //TODO consider "dropdown" for different templates
  internal static void CreateSpecWithoutDiscovery( string specPath ) {
    var networkBuilder = new NetworkBuilder();

    networkBuilder.AddSubnet( new CidrBlock( "192.168.1.0/24" ), id: "main-lan" );
    networkBuilder.AddSubnet( new CidrBlock( "192.168.100.0/24" ), id: "iot" );
    networkBuilder.AddSubnet( new CidrBlock( "192.168.200.0/24" ), id: "guest" );

    networkBuilder.AddDevice( [new IpV4Address( "192.168.1.10" )], id: "router", enabled: null, state: null );
    networkBuilder.AddDevice( [new IpV4Address( "192.168.1.20" )], id: "nas", enabled: null, state: null );
    networkBuilder.AddDevice( [new IpV4Address( "192.168.1.30" )], id: "server", enabled: null, state: null );
    networkBuilder.AddDevice( [new IpV4Address( "192.168.1.40" )], id: "desktop", enabled: null, state: null );
    networkBuilder.AddDevice( [new IpV4Address( "192.168.1.50" )], id: "laptop", enabled: null, state: null );

    networkBuilder.AddDevice( [new IpV4Address( "192.168.100.10" )], id: "smart-tv", enabled: null, state: null );
    networkBuilder.AddDevice( [new IpV4Address( "192.168.100.20" )], id: "security-camera", enabled: null,
      state: null );
    networkBuilder.AddDevice( [new IpV4Address( "192.168.100.30" )], id: "smart-switch", enabled: null, state: null );

    networkBuilder.AddDevice( [new IpV4Address( "192.168.200.100" )], id: "guest-device", enabled: null, state: null );

    networkBuilder.WriteToFile( specPath );
  }

  private static void CreateSpec(
    List<CidrBlock> subnets,
    List<DeclaredDevice> devices,
    string specPath
  ) {
    var networkBuilder = new NetworkBuilder();

    foreach ( var subnet in subnets ) {
      networkBuilder.AddSubnet( subnet );
    }

    var declaredDevices = devices
      .OrderBy( d => d.Get( AddressType.IpV4 ), StringComparison.OrdinalIgnoreCase.WithNaturalSort() );

    var no = 1;
    foreach ( var device in declaredDevices ) {
      networkBuilder.AddDevice( addresses: [..device.Addresses], id: $"device-{no++}", enabled: null, state: null );
    }

    networkBuilder.WriteToFile( specPath );
  }

  private class InitOptions {
    public required string Name {
      get;
      init;
    }

    // TODO Enable when supporting environments.
    /*public bool WithEnv {
      get;
      init;
    }*/

    public required bool Overwrite {
      get;
      init;
    }

    public required bool Discover {
      get;
      init;
    }
  }
}