using System.CommandLine;
using Drift.Cli.Commands.Common;
using Drift.Cli.Output.Abstractions;

namespace Drift.Cli.Commands;

internal abstract class CommandBase<TParameters> : Command where TParameters : DefaultParameters {
  protected IOutputManager Output {
    get;
    private set;
  } = null!;

  protected CommandBase(
    string name,
    string description,
    Func<ParseResult, IOutputManager> outputManagerFactory,
    Func<ParseResult, TParameters> parametersFactory
  ) : base( name, description ) {
    Add( CommonParameters.Options.Verbose );
    // TODO re-intro when fixed
    // AddOption( GlobalParameters.Options.VeryVerbose );
    Add( CommonParameters.Options.OutputFormatOption );
    Add( CommonParameters.Arguments.SpecOptional );

    SetAction( ( parseResult, cancellationToken ) => {
        Output ??= outputManagerFactory( parseResult ); // Only null if not set in this class

        return Invoke( cancellationToken, parametersFactory( parseResult ) );
      }
    );
  }

  protected abstract Task<int> Invoke( CancellationToken cancellationToken, TParameters parameters );
}