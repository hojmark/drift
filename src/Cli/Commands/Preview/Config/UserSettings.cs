namespace Drift.Cli.Commands.Preview.Config;

internal class UserSettings {
  internal bool Emojis {
    get;
    set;
  } = true;

  internal bool Color {
    get;
    set;
  } = true;

  //TODO ask during first run
  internal bool Telemetry {
    get;
    set;
  } = false;

  internal string? DefaultEnv {
    get;
    set;
  }

  internal string? DefaultSpec {
    get;
    set;
  }

  internal static string FilePath =>
    Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".config", "drift",
      "drift.settings.json" );

  internal static UserSettings Load() {
    var path = FilePath;
    if ( !File.Exists( path ) ) return new UserSettings();
    //TODO return YamlSerializer.Deserialize<UserSettings>(File.ReadAllText(path)) ?? new UserSettings();
    return new UserSettings();
  }

  internal void Save() {
    var dir = Path.GetDirectoryName( FilePath );
    if ( dir != null ) Directory.CreateDirectory( dir );
    //TODO File.WriteAllText(FilePath, YamlSerializer.Serialize(this));
  }

  internal void Reset() {
    File.Delete( FilePath );
  }
}