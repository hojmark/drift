using System.Net;
using Drift.Cli.Abstractions;
using Drift.Cli.Settings.Tests;
using Drift.Cli.Tests.Utils;
using Drift.Common.IO;
using Drift.TestUtilities.Hosts;
using Drift.TestUtilities.IO;

namespace Drift.Cli.Tests.Commands;

/// <summary>
/// Server/agent configuration resolution: defaults &lt; config file &lt; environment variables &lt; command-line options.
/// </summary>
internal sealed class HostConfigurationTests {
  private const string RequestLogLine = "HTTP request started";

  [TestCase( null, true )]
  [TestCase( true, true )]
  [TestCase( false, false )]
  public async Task Server_RequestLogging_FollowsConfigFile( bool? enabledInFile, bool expectLogged ) {
    var settingsLocation = CreateSettingsLocation();
    if ( enabledInFile is { } enabled ) {
      await WriteServerConfigAsync(
        settingsLocation,
        $$"""{ "requestLogging": { "enabled": {{enabled.ToString().ToLowerInvariant()}} } }"""
      );
    }

    var output = await RunServerAndRequestAsync( settingsLocation );

    Assert.That( output, expectLogged ? Does.Contain( RequestLogLine ) : Does.Not.Contain( RequestLogLine ) );
  }

  [Test]
  [NonParallelizable] // Mutates process environment
  public async Task Server_EnvironmentVariable_OverridesConfigFile() {
    var settingsLocation = CreateSettingsLocation();
    await WriteServerConfigAsync( settingsLocation, """{ "requestLogging": { "enabled": true } }""" );

    const string variable = EnvVarPrefix.Server + "REQUESTLOGGING__ENABLED";
    Environment.SetEnvironmentVariable( variable, "false" );
    try {
      var output = await RunServerAndRequestAsync( settingsLocation );

      Assert.That( output, Does.Not.Contain( RequestLogLine ) );
    }
    finally {
      Environment.SetEnvironmentVariable( variable, null );
    }
  }

  [Test]
  public async Task Server_PortFromConfigFile_IsUsed_AndCommandLineOptionOverridesIt() {
    var settingsLocation = CreateSettingsLocation();
    var filePort = TcpUtils.GetFreePort();
    var cliPort = TcpUtils.GetFreePort();
    await WriteServerConfigAsync( settingsLocation, $$"""{ "port": {{filePort}}, "listenForAgents": false }""" );

    var fromFile = await RunUntilStartedAsync( Role.Server, string.Empty, settingsLocation );
    var fromCli = await RunUntilStartedAsync( Role.Server, $"--port {cliPort}", settingsLocation );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( fromFile, Does.Contain( $"Control API port: {filePort} (HTTP)" ) );
      Assert.That( fromFile, Does.Contain( "Not listening for inbound agent connections" ) );
      Assert.That( fromCli, Does.Contain( $"Control API port: {cliPort} (HTTP)" ) );
    }
  }

  [TestCase( """{ "requestLoging": { "enabled": false } }""", "requestLoging" )]
  [TestCase( """{ "port": "not-a-port" }""", "not-a-port" )]
  [TestCase( """{ "port": 7000, "agentPort": 7000 }""", "must differ" )]
  [TestCase( """{ "port": """, "server.json" )]
  public async Task Server_InvalidConfigFile_Fails( string json, string expectedInMessage ) {
    var settingsLocation = CreateSettingsLocation();
    await WriteServerConfigAsync( settingsLocation, json );

    var result = await DriftTestCli.InvokeAsync( "server start", settingsLocation: settingsLocation );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( result.ExitCode, Is.EqualTo( ExitCodes.GeneralError ) );
      Assert.That( result.Output + result.Error.ToString(), Does.Contain( expectedInMessage ) );
    }
  }

  [Test]
  public async Task Server_SchemaProperty_IsAllowed() {
    var settingsLocation = CreateSettingsLocation();
    var port = TcpUtils.GetFreePort();
    await WriteServerConfigAsync(
      settingsLocation,
      $$"""{ "$schema": "https://example.com/schema.json", "port": {{port}}, "listenForAgents": false }"""
    );

    var output = await RunUntilStartedAsync( Role.Server, string.Empty, settingsLocation );

    Assert.That( output, Does.Contain( $"Control API port: {port} (HTTP)" ) );
  }

  [Test]
  public async Task Agent_PortFromConfigFile_IsUsed() {
    var settingsLocation = CreateSettingsLocation();
    var port = TcpUtils.GetFreePort();
    await File.WriteAllTextAsync( settingsLocation.AgentConfigFile, $$"""{ "port": {{port}} }""" );

    var output = await RunUntilStartedAsync( Role.Agent, string.Empty, settingsLocation );

    Assert.That( output, Does.Contain( $"Port: {port} (gRPC)" ) );
  }

  [TestCase( null, false )]
  [TestCase( true, true )]
  public async Task Agent_RequestLogging_FollowsConfigFile( bool? enabledInFile, bool expectLogged ) {
    var settingsLocation = CreateSettingsLocation();
    var port = TcpUtils.GetFreePort();
    var requestLogging = enabledInFile is { } enabled
      ? $$""", "requestLogging": { "enabled": {{enabled.ToString().ToLowerInvariant()}} }"""
      : string.Empty;
    await File.WriteAllTextAsync( settingsLocation.AgentConfigFile, $$"""{ "port": {{port}}{{requestLogging}} }""" );

    using var tcs = new CancellationTokenSource();
    var command = await DriftTestCli.StartAgentAsync(
      string.Empty,
      tcs.Token,
      settingsLocation: settingsLocation,
      dataLocation: new TemporaryDriftDataLocation()
    );

    // gRPC endpoint: HTTP/2 without TLS
    using var client = new HttpClient {
      DefaultRequestVersion = HttpVersion.Version20, DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
    };
    using var response = await client.GetAsync( $"http://127.0.0.1:{port}/" );

    await tcs.CancelAsync();
    var result = await command.Completion;

    Assert.That(
      result.Output.ToString(),
      expectLogged ? Does.Contain( RequestLogLine ) : Does.Not.Contain( RequestLogLine )
    );
  }

  private enum Role {
    Server,
    Agent
  }

  private static IDriftSettingsLocation CreateSettingsLocation() {
    var location = new TemporaryDriftSettingsLocation();
    Directory.CreateDirectory( location.Directory );
    return location;
  }

  private static Task WriteServerConfigAsync( IDriftSettingsLocation location, string json ) {
    return File.WriteAllTextAsync( location.ServerConfigFile, json );
  }

  /// <summary>
  /// Starts <c>server start</c>/<c>agent start</c> with <paramref name="args"/>, stops it once ready and returns the output.
  /// </summary>
  private static async Task<string> RunUntilStartedAsync(
    Role role,
    string args,
    IDriftSettingsLocation settingsLocation
  ) {
    using var tcs = new CancellationTokenSource();
    var dataLocation = new TemporaryDriftDataLocation();

    var command = role == Role.Server
      ? await DriftTestCli.StartServerAsync(
        args,
        tcs.Token,
        dataLocation: dataLocation,
        settingsLocation: settingsLocation
      )
      : await DriftTestCli.StartAgentAsync(
        args,
        tcs.Token,
        dataLocation: dataLocation,
        settingsLocation: settingsLocation
      );

    await tcs.CancelAsync();
    var result = await command.Completion;
    Assert.That( result.ExitCode, Is.EqualTo( ExitCodes.Success ), result.Error.ToString() );
    return result.Output.ToString() ?? string.Empty;
  }

  private static async Task<string> RunServerAndRequestAsync( IDriftSettingsLocation settingsLocation ) {
    using var tcs = new CancellationTokenSource();
    var port = TcpUtils.GetFreePort();
    var command = await DriftTestCli.StartServerAsync(
      $"--port {port} --no-agent-port",
      tcs.Token,
      dataLocation: new TemporaryDriftDataLocation(),
      settingsLocation: settingsLocation
    );

    using var client = new HttpClient();
    using var response = await client.GetAsync( $"http://127.0.0.1:{port}/api/v1/nonexistent" );

    await tcs.CancelAsync();
    var result = await command.Completion;
    return result.Output.ToString() ?? string.Empty;
  }
}