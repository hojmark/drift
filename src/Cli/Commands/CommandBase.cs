using System.CommandLine;
using Drift.Cli.Commands.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Cli.Commands;

internal abstract class CommandBase<TParameters, THandler> : Command
  where TParameters : DefaultParameters
  where THandler : ICommandHandler<TParameters> {
  protected CommandBase( string name, string description, IServiceProvider provider ) : base( name, description ) {
    Add( CommonParameters.Options.Verbose );
    // TODO re-intro when fixed
    // AddOption( GlobalParameters.Options.VeryVerbose );
    Add( CommonParameters.Options.OutputFormatOption );
    Add( CommonParameters.Arguments.SpecOptional );

    SetAction( ( parseResult, cancellationToken ) => {
      using var scope = provider.CreateScope();
      var serviceProvider = scope.ServiceProvider;

      serviceProvider.GetRequiredService<ParseResultHolder>().ParseResult = parseResult;

      var handler = serviceProvider.GetRequiredService<THandler>();
      var parameters = CreateParameters( parseResult );

      return handler.Invoke( parameters, cancellationToken );
    } );
  }

  protected abstract TParameters CreateParameters( ParseResult result );
}