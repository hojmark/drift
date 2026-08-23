using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Parameters;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Settings.Serialization;
using Drift.Cli.Settings.V1_preview;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Cli.Commands.Common.Commands;

internal abstract class CommandBase<TParameters, THandler> : Command
  where TParameters : BaseParameters
  where THandler : ICommandHandler<TParameters> {
  protected CommandBase(
    string name,
    string description,
    IServiceProvider provider,
    bool includeSpecArgument = true
  ) : base( name, description ) {
    Add( CommonParameters.Options.Verbose );
    // TODO re-intro when fixed
    // AddOption( GlobalParameters.Options.VeryVerbose );
    Add( CommonParameters.Options.OutputFormat );

    // TODO hack
    if ( includeSpecArgument ) {
      Add( CommonParameters.Arguments.Spec );
    }

    SetAction( async ( parseResult, cancellationToken ) => {
      await using var scope = provider.CreateAsyncScope();
      var serviceProvider = scope.ServiceProvider;

      serviceProvider.GetRequiredService<ParseResultHolder>().ParseResult = parseResult;

      var output = serviceProvider.GetRequiredService<IOutputManager>();
      // Uses a null logger: the --output option's default value factory already reads (and logs errors for)
      // the settings file once per invocation; avoid surfacing the same error a second time here.
      // TODO read cached settings instead?
      var settingsLocation = serviceProvider.GetRequiredService<ISettingsLocationProvider>();
      var activeEnvironment = CliSettings.Read( NullLogger.Instance, settingsLocation ).ActiveEnvironment;
      output.WriteEnvironmentHeader( activeEnvironment );

      var handler = serviceProvider.GetRequiredService<THandler>();

      TParameters parameters;
      try {
        parameters = CreateParameters( parseResult );
      }
      catch ( ArgumentException e ) {
        output.Normal.WriteLineError( $"✗ {e.Message}" );
        return ExitCodes.GeneralError;
      }

      return await handler.Invoke( parameters, cancellationToken );
    } );
  }

  protected abstract TParameters CreateParameters( ParseResult result );
}
