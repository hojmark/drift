using System.CommandLine;
using System.Globalization;
using System.Text;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Global;
using Drift.Cli.Commands.Scan;
using Drift.Cli.Commands.Scan.Subnet;
using Drift.Cli.Output;
using Drift.Cli.Output.Abstractions;
using Drift.Diff.Domain;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Extensions;
using Drift.Domain.Scan;
using Drift.Parsers.SpecYaml;
using Drift.Utils;
using Humanizer;
using Microsoft.Extensions.Logging;
using NaturalSort.Extension;
using Spectre.Console;
using Environment = System.Environment;

namespace Drift.Cli.Commands.Init;

internal class InitCommand : Command {
  internal InitCommand( ILoggerFactory loggerFactory ) : base( "init", "Create a network spec" ) {
    // Intended for testing (although I should maybe look into a better way to do this e.g. Linux expect)
    var forceModeOption = new Option<ForceMode?>(
      "--force-mode",
      "(HIDDEN) Force mode"
    ) { Arity = ArgumentArity.ZeroOrOne, IsHidden = true };
    AddOption( forceModeOption );

    /*var withEnvOption = new Option<bool>(
      "--with-env",
      "Also create an environment config alongside the spec"
    ) { Arity = ArgumentArity.ZeroOrOne };*/

    // Should perform the same default scan as the scan command to give a good first impression
    // TODO structure code so this always happens
    var discoverOption = new Option<bool?>(
      "--discover",
      "Populate with devices and subnets discovered in a network scan"
    ) { Arity = ArgumentArity.ZeroOrOne };
    AddOption( discoverOption );

    var overwriteOption = new Option<bool?>(
      "--overwrite",
      "Overwrite existing file"
    ) { Arity = ArgumentArity.ZeroOrOne };
    AddOption( overwriteOption );

    AddOption( GlobalParameters.Options.Verbose );
    //AddOption( GlobalParameters.Options.VeryVerbose );

    AddOption( GlobalParameters.Options.OutputFormatOption );

    //TODO support examples
    /*initCommand.WithExamples(
      "drift init     (interactive)",
      "drift init main-site",
      "drift init main-site --discover --with-env"
    );*/

    AddArgument( GlobalParameters.Arguments.SpecOptional );

    this.SetHandler(
      CommandHandler,
      new ConsoleOutputManagerBinder( loggerFactory ),
      GlobalParameters.Arguments.SpecOptional,
      GlobalParameters.Options.OutputFormatOption,
      overwriteOption,
      discoverOption,
      forceModeOption
    );
  }

  private static async Task<int> CommandHandler(
    IOutputManager output,
    FileInfo? name,
    GlobalParameters.OutputFormat outputFormat,
    bool? overwrite,
    bool? discover,
    ForceMode? forceMode
  ) {
    var isInteractive = IsInteractiveMode( forceMode, name, overwrite, discover );

    if ( isInteractive && outputFormat != GlobalParameters.OutputFormat.Normal ) {
      throw new ArgumentException( "Interactive mode is not supported with non-normal output format" );
    }

    output.Log.LogDebug( "Running init command" );
    // TODO skip emojis in output if redirected?

    var initOptions = isInteractive
      ? RunInteractive( output.Normal )
      : RunNonInteractive( output, name?.Name, overwrite, discover );

    if ( initOptions == null ) {
      return ExitCodes.Error;
    }

    var success = await Initialize( output, initOptions );

    if ( !success ) {
      return ExitCodes.Error;
    }

    if ( success && isInteractive ) {
      AnsiConsole.WriteLine();
      AnsiConsole.MarkupLine( $"💡\uFE0F Next: Try [bold][green]drift scan {initOptions.Name}[/][/]" );
    }

    output.Log.LogDebug( "Init command completed" );

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

    AnsiConsole.MarkupLine( "[bold]📡\uFE0F Welcome to Drift! Let's set up a new spec.[/]" );

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
      output.Normal.WriteError( "❌\uFE0F Name is required" );
      output.Log.LogError( "Name is required" );
      return null;
    }

    return new InitOptions { Name = name, Overwrite = overwrite ?? false, Discover = discover ?? false };
  }


  private static async Task<bool> Initialize( IOutputManager output, InitOptions options ) {
    try {
      var specPath = Path.Combine( ".", $"{options.Name}.spec.yaml" );
      var envPath = Path.Combine( ".", $"{options.Name}.env.yaml" );

      if ( !options.Overwrite && File.Exists( specPath ) ) {
        output.Normal.WriteError( "❌\uFE0F Spec file already exists: " );
        output.Normal.WriteLineError( ConsoleExtensions.Text.Bold( $"{specPath}" ) );

        output.Log.LogError( "Spec file already exists: {SpecPath}", specPath );

        return false;
      }

      // SCAN
      // TODO centralize logic between scancommand and this
      ISubnetProvider subnetProvider = new InterfaceSubnetProvider( output );
      var subnets = subnetProvider.Get();
      var scanner = new PingNetworkScanner( output ); // Or inject via DI

      ScanResult? scanResult = null;

      //TODO create unit test for this
      output.Normal.WriteLineVerbose(
        "Found subnets: " + string.Join( ", ",
          subnets.Select( s =>
            s + " (" + IpNetworkUtils.GetIpRangeCount( IpNetworkUtils.GetNetmask( s.PrefixLength ) ) + " addresses, " +
            CalculateScanDuration( s.PrefixLength,
              PingNetworkScanner.MaxPingsPerSecond ) /*.Humanize( 2, CultureInfo.InvariantCulture )*/ +
            " estimated scan time" +
            ")"
          )
        )
      );

      if ( options.Discover ) {
        if ( output.Is( GlobalParameters.OutputFormat.Normal ) ) {
          await AnsiConsole
            .Status()
            .StartAsync( "Scanning network ...", async ctx => {
              scanResult = await scanner.ScanAsync( subnets.First() );
              await Task.Delay( 1500 );
            } );
        }

        if ( output.Is( GlobalParameters.OutputFormat.Log ) ) {
          var lastLogTime = DateTime.MinValue;
          var completedTasks = new HashSet<string>();

          //TODO note: subnets.FIRST() !!! support multiple subnets !!!
          scanResult = await scanner.ScanAsync( subnets.First(), onProgress: progressReport => {
            ScanCommand.UpdateProgressLog( progressReport, output, ref lastLogTime, ref completedTasks );
          }, cancellationToken: CancellationToken.None );
        }

        if ( scanResult == null ) {
          output.Log.LogDebug( "Scan result is null" );
          output.Normal.WriteLineError( "Scan result is null" );
          return false;
        }

        output.Log.LogInformation( "Scan completed" );
        output.Log.LogDebug( "Found {Count} devices", scanResult.DiscoveredDevices.Count() );
      }

      output.Log.LogInformation( "Writing spec..." );

      CreateSpec( scanResult, subnetProvider, specPath );

      var fullPath = Path.GetFullPath( specPath );

      if ( output.Is( GlobalParameters.OutputFormat.Normal ) ) {
        output.Normal.Write( "✅\uFE0F Created spec: " );
        output.Normal.WriteLine( ConsoleExtensions.Text.Bold( $"{fullPath}" ) );
      }

      if ( output.Is( GlobalParameters.OutputFormat.Log ) ) {
        output.Log.LogInformation( "Created spec: {SpecPath}", specPath );
      }

      return true;
    }
    catch ( Exception e ) {
      //TODO create generic catch for all commands
      output.Normal.WriteLine( e.ToString() );
      output.Normal.WriteLineError( e.StackTrace );
      output.Log.LogError( e, "Unexpected error" );
      throw;
    }
  }

  //TODO move somewhere else
  public static TimeSpan CalculateScanDuration( int prefixLength, double scansPerSecond ) {
    double hostCount = IpNetworkUtils.GetIpRangeCount( IpNetworkUtils.GetNetmask( prefixLength ) );
    double totalSeconds = hostCount / scansPerSecond;
    return TimeSpan.FromSeconds( totalSeconds );
  }

  private static void CreateSpec( ScanResult? scanResult, ISubnetProvider subnetProvider, string specPath ) {
    var declaredDevices = scanResult?.DiscoveredDevices.OrderBy( d => d.Get( AddressType.IpV4 ),
      StringComparison.OrdinalIgnoreCase.WithNaturalSort() ).ToDeclared();
    var no = 1;
    foreach ( var declaredDevice in declaredDevices ?? [] ) {
      declaredDevice.Id = $"device-{no++}";
      declaredDevice.Enabled = null; // Will then default to true
    }

    var inventory = new Inventory {
      Network = new Network {
        Subnets = subnetProvider.Get().DistinctBy( s => s.NetworkAddress ).Select( s => new DeclaredSubnet {
          //TODO possible for Network to be Cidr (need to update (de)serialization)
          Network = s.ToString(), Enabled = null, Id = "subnet-1"
        } ).ToList(),
        Devices = declaredDevices ?? []
      }
    };

    var yamlContents = YamlConverter.Serialize( inventory );
    File.WriteAllText( specPath, yamlContents, Encoding.UTF8 );
  }

  internal static class ConsoleExtensions {
    internal static class Text {
      internal static string Bold( string text ) => $"\x1b[1m{text}\x1b[0m";
    }
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