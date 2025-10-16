namespace Drift.Domain.ExecutionEnvironment;

public interface IExecutionEnvironmentProvider {
  public DriftExecutionEnvironment Get();
}