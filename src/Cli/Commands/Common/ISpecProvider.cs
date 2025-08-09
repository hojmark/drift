using Drift.Domain;

namespace Drift.Cli.Commands.Common;

public interface ISpecProvider {
  Task<(FileInfo? Path, string Contents)?> GetAsync( string? name );

  Task<Inventory> DeserializeAsync( string? name );
}