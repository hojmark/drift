using Drift.Domain;

namespace Drift.Cli.Commands.Common;

public class FileSystemSpecProvider : ISpecProvider {
  private readonly SpecFilePathResolver _specPathResolver;

  public FileSystemSpecProvider(
    //TODO decide on name
    SpecFilePathResolver specPathResolver
  ) {
    _specPathResolver = specPathResolver;
  }

  public async Task<(FileInfo? Path, string Contents)?> GetAsync( string? name ) {
    var path = _specPathResolver.Resolve( name );

    if ( path == null ) {
      return null;
    }

    var yamlContent = await File.ReadAllTextAsync( path.FullName );

    return ( path, yamlContent );
  }
  
  public async Task<Inventory> DeserializeAsync( string? name ) {
    return null;
  }
}