using Drift.Cli.Commands.Common.Parameters;

namespace Drift.Cli.Commands.Common.Commands;

internal interface ICommandHandler<in TParameters> where TParameters : BaseParameters {
  Task<int> Invoke( TParameters parameters, CancellationToken cancellationToken );
}