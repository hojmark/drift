using Drift.Domain;

namespace Drift.Cli.SpecFile;

internal interface ISpecFileProvider {
  Task<Inventory?> GetDeserializedAsync( FileInfo? specFile );
}