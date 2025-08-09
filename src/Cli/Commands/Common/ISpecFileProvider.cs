using Drift.Domain;

namespace Drift.Cli.Commands.Common;

public interface ISpecFileProvider {
  Task<Inventory?> GetDeserializedAsync( FileInfo? specFile );
}