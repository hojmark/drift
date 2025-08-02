namespace Drift.Cli.Commands.Common;

internal interface ICommandHandler<in TParameters> where TParameters : DefaultParameters {
  Task<int> Invoke( TParameters parameters, CancellationToken cancellationToken );
}