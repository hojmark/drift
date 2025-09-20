using Drift.Domain;

namespace Drift.Cli.Commands.Common;

internal interface ISpecFileProvider {
  Task<Inventory?> GetDeserializedAsync( FileInfo? specFile );
}