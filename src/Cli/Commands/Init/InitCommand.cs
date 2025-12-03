using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Commands.Init.Helpers;
using Drift.Cli.Commands.Scan.NonInteractive;
using Drift.Cli.Presentation.Console;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Presentation.Prompts;
using Drift.Cli.Presentation.Rendering;
using Drift.Common.Network;
using Drift.Domain.Scan;
using Drift.Scanning.Subnets.Interface;
using Spectre.Console;

namespace Drift.Cli.Commands.Init;

internal class InitCommand : CommandBase<InitParameters, InitCommandHandler> {
  // Intended for testing (although I should maybe look into a better way to do this e.g., Linux 'expect')
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

    // TODO support examples
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
  // TODO skip emojis in output if redirected?
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

    if ( isInteractive ) {
      output.Normal.WriteLine();
      output.Normal.WriteLineCTA( $"{Chars.Bulb} Next step: Run", $"drift scan {initOptions.Name}" );
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

  private static InitOptions RunInteractive(
    INormalOutput console
    // How to support cancellation of the prompt (Console.ReadLine)?
    /*, CancellationToken cancellationToken */
  ) {
    // TODO first run logic
    // var driftStatePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".drift" );

    console.WriteLine();
    console
      .GetAnsiConsole()
      .MarkupLine( $"[bold]{Chars.SatelliteAntenna} Welcome to Drift! Let's set up a new spec.[/]" );
    console.WriteLine();

    var name = console.PromptString( $"{Chars.Globe} Name your network", "main-site" );
    // var withEnv = PromptBool( "🗃\uFE0F  Create environment file too?", defaultOption: PromptOption.Yes );
    var discover = console.PromptBool(
      $"{Chars.MagnifyingGlass} Run a discovery scan to auto-fill with devices and subnets?",
      defaultOption: PromptOption.Yes
    );

    var specPath = GetSpecPath( name );
    var envPath = GetEnvPath( name );

    var overwrite = ( File.Exists( specPath ) || File.Exists( envPath ) ) && console.PromptBool(
      $"{Chars.Warning}  Spec file already exist. Overwrite it?", // TODO pluralize when supporting env files too
      defaultOption: PromptOption.No
    );

    console.WriteLine();

    return new InitOptions { Name = name, Overwrite = overwrite, Discover = discover };
  }

  private static InitOptions? RunNonInteractive(
    IOutputManager output,
    string? name,
    bool? overwrite,
    bool? discover
  ) {
    if ( string.IsNullOrWhiteSpace( name ) ) {
      output.Normal.WriteError( $"{Chars.Cross} Name is required" );
      output.Log.LogError( "Name is required" );
      return null;
    }

    return new InitOptions { Name = name, Overwrite = overwrite ?? false, Discover = discover ?? false };
  }

  private static string GetSpecPath( string name ) => Path.GetFullPath( $"{name}.spec.yaml" );

  private static string GetEnvPath( string name ) => Path.GetFullPath( $"{name}.env.yaml" );

  private async Task<bool> Initialize( InitOptions options ) {
    try {
      var specPath = GetSpecPath( options.Name );
      // var envPath = GetEnvPath( options.Name );

      if ( !ValidateSpecOverwrite( specPath, options.Overwrite ) ) {
        return false;
      }

      var scanOptions = new NetworkScanOptions {
        Cidrs = ( await interfaceSubnetProvider.GetAsync() ).Select( subnet => subnet.Cidr ).ToList()
      };

      LogSubnetDetails( scanOptions );

      if ( options.Discover ) {
        var result = await PerformScanAsync( scanOptions );

        output.Log.LogInformation( "Scan completed" );
        output.Log.LogDebug(
          "Found {Count} devices",
          result.Subnets.Select( subnet => subnet.DiscoveredDevices.Count ).Sum()
        );
        output.Log.LogInformation( "Writing spec: {SpecPath}", specPath );

        SpecFactory.CreateFromScan( result, specPath );
      }
      else {
        output.Normal.WriteLineVerbose( "No discovery, writing template spec" );

        output.Log.LogDebug( "No discovery, writing template spec" );
        output.Log.LogInformation( "Writing spec: {SpecPath}", specPath );

        SpecFactory.CreateFromTemplate( specPath );
      }

      LogSpecCreated( specPath );

      return true;
    }
    // TODO create generic catch for all commands
#pragma warning disable S2139
    catch ( Exception e ) {
      output.Normal.WriteLineError( "Unexpected error" );
      output.Normal.WriteLineError( e.ToString() );
      output.Log.LogError( e, "Unexpected error" );
      throw;
    }
#pragma warning restore S2139
  }

  private async Task<NetworkScanResult> PerformScanAsync( NetworkScanOptions request ) {
    if ( output.Is( OutputFormat.Normal ) ) {
      return await AnsiConsole.Status().StartAsync(
        "Scanning network ...",
        async _ => await scanner.ScanAsync( request, output.GetLogger() )
      );
    }

    if ( output.Is( OutputFormat.Log ) ) {
      output.Log.LogInformation( "Scanning network..." );

      var lastLogTime = DateTime.MinValue;

      void LogProgress( object? _, NetworkScanResult r ) {
        NonInteractiveUi.UpdateProgressDebounced(
          r.Progress,
          progress => output.Log.LogInformation( "{TaskName}: {CompletionPct}", "Ping Scan", progress ),
          ref lastLogTime
        );
      }

      try {
        scanner.ResultUpdated += LogProgress;
        return await scanner.ScanAsync( request, output.GetLogger() );
      }
      finally {
        scanner.ResultUpdated -= LogProgress;
      }
    }

    throw new NotImplementedException();
  }

  private bool ValidateSpecOverwrite( string path, bool overwrite ) {
    if ( !File.Exists( path ) ) {
      return true;
    }

    if ( overwrite ) {
      output.Normal.WriteLineVerbose( $"Spec file already exists: {path} (overwriting)" );
      output.Log.LogDebug( "Spec file already exists: {SpecPath} (overwriting)", path );
      return true;
    }

    output.Normal.WriteError( $"{Chars.Cross} Spec file already exists:" );
    output.Normal.WriteLineError( TextHelper.Bold( path ) );
    output.Log.LogError( "Spec file already exists: {SpecPath}", path );
    return false;
  }

  private void LogSpecCreated( string specPath ) {
    var fullPath = Path.GetFullPath( specPath );

    output.Normal.Write( $"{Chars.Checkmark}", ConsoleColor.Green );
    output.Normal.Write( "  Spec created " );
    output.Normal.WriteLine( TextHelper.Bold( $"{fullPath}" ) );

    output.Log.LogInformation( "Spec created: {SpecPath}", specPath );
  }

  private void LogSubnetDetails( NetworkScanOptions scanOptions ) {
    // TODO create unit test for this
    output.Normal.WriteLineVerbose(
      "Found subnets: " + string.Join(
        ", ",
        scanOptions.Cidrs.Select( cidr =>
          cidr + " (" + IpNetworkUtils.GetIpRangeCount( cidr ) +
          " addresses, " +
          scanOptions.EstimatedDuration( cidr ) /* TODO .Humanize( 2, CultureInfo.InvariantCulture )*/ +
          " estimated scan time" +
          ")"
        )
      )
    );
  }

  private sealed class InitOptions {
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