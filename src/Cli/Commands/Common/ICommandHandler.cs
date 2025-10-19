namespace Drift.Cli.Commands.Common;

internal interface ICommandHandler<in TParameters> where TParameters : BaseParameters {
  Task<int> Invoke( TParameters parameters, CancellationToken cancellationToken );
}