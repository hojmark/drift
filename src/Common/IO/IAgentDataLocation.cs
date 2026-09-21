namespace Drift.Common.IO;

public interface IAgentDataLocation {
  string Directory {
    get;
  }

  void EnsureCreated();
}