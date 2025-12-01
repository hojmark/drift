using System.CommandLine;
using Drift.Cli.Commands.Common.Parameters;

namespace Drift.Cli.Commands.Common.Commands;

internal abstract class CommandBase<TParameters, THandler> : Command
  where TParameters : BaseParameters
  where THandler : ICommandHandler<TParameters> {
  protected CommandBase( string name, string description, IServiceProvider provider ) : base( name, description ) {
    Add( CommonParameters.Options.Verbose );
    // TODO re-intro when fixed
    // AddOption( GlobalParameters.Options.VeryVerbose );
    Add( CommonParameters.Options.OutputFormat );
    Add( CommonParameters.Arguments.Spec );

    SetAction( async ( parseResult, cancellationToken ) => {
      await using var scope = provider.CreateAsyncScope();
      var serviceProvider = scope.ServiceProvider;

      serviceProvider.GetRequiredService<ParseResultHolder>().ParseResult = parseResult;

      var handler = serviceProvider.GetRequiredService<THandler>();
      var parameters = CreateParameters( parseResult );

      return await handler.Invoke( parameters, cancellationToken );
    } );
  }

  protected abstract TParameters CreateParameters( ParseResult result );
}