namespace Drift.Build.Utilities.Versioning;

/*public class ReleaseInfo : IReleaseInfo {
  private readonly IVersioningStrategy _parent;

  public ReleaseInfo( IVersioningStrategy parent ) {
    _parent = parent;
  }

  public async Task<string> GetGitTagAsync() {
    var version = await _parent.GetVersionAsync();
    return "v" + version.WithoutMetadata();
  }

  public abstract Task<string> GetReleaseNameAsync();
}*/