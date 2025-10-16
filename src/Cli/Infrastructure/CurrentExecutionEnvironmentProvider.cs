using Drift.Domain.ExecutionEnvironment;

namespace Drift.Cli.Infrastructure;

internal class CurrentExecutionEnvironmentProvider : IExecutionEnvironmentProvider {
  public DriftExecutionEnvironment Get() {
    return ExecutionEnvironment.GetCurrent();
  }
}