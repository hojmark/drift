using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.SpecFile;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Drift.Cli.Tests;

internal sealed class SpecFilePathResolverTests {
  private string? _originalHome;
  private string _tempHome;
  private const string _homeEnvVar = "HOME";
  private string? _osUserProfile;

  [OneTimeSetUp]
  public void SetupHomeDir() {
    _tempHome = Path.Combine( Path.GetTempPath(), "fake-home-" + Guid.NewGuid().ToString() );
    Directory.CreateDirectory( _tempHome );

    _originalHome = Environment.GetEnvironmentVariable( _homeEnvVar );
    Environment.SetEnvironmentVariable( _homeEnvVar, _tempHome );
    _osUserProfile = _tempHome;
  }

  [OneTimeTearDown]
  public void TeardownHomeDir() {
    Environment.SetEnvironmentVariable( _homeEnvVar, _originalHome );
    Directory.Delete( _tempHome, true );
  }

  /**
   * Checks the environment override used by the test fixture
   */
  [Test]
  public void UserHomeFolderEnv_ResolvesToFakeHomeFolder_AsExpected() {
    var actualHome = Environment.GetFolderPath( Environment.SpecialFolder.UserProfile );

    Assert.That( actualHome, Contains.Substring( "fake-home-" ) );
    Assert.That( Directory.Exists( actualHome ), Is.True );
  }


  [Test]
  public void Resolve_ExactFileName_ReturnsFile() {
    var tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
    Directory.CreateDirectory( tempDir );
    var fileName = "mytest.yaml";
    var filePath = CreateTempFile( tempDir, fileName );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), tempDir );
    var result = resolver.Resolve( fileName );

    Assert.That( result, Is.Not.Null );
    Assert.That( result.FullName, Is.EqualTo( filePath ) );

    Directory.Delete( tempDir, true );
  }

  [Test]
  public void Resolve_NameWithTilde_ExpandsToHomeDirectory() {
    var home = Environment.GetFolderPath( Environment.SpecialFolder.UserProfile );
    var fileName = "sample.yaml";
    var filePath = CreateTempFile( home, fileName );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), Directory.GetCurrentDirectory() );
    var result = resolver.Resolve( "~" + Path.DirectorySeparatorChar + fileName );

    Assert.That( result, Is.Not.Null );
    Assert.That( result.FullName, Is.EqualTo( Path.Combine( home, fileName ) ) );

    File.Delete( filePath );
  }

  [Test]
  public void Resolve_NameIsTilde_ReturnsDefaultsFromHome() {
    var home = Environment.GetFolderPath( Environment.SpecialFolder.UserProfile );
    var specName = "drift.spec.yaml";
    var filePath = CreateTempFile( home, specName );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), Directory.GetCurrentDirectory() );
    var result = resolver.Resolve( "~" );

    Assert.That( result, Is.Not.Null );
    Assert.That( result.FullName, Is.EqualTo( Path.Combine( home, specName ) ) );

    File.Delete( filePath );
  }

  [Test]
  public void Resolve_NamePlusSpecYaml_ReturnsSpecFile() {
    var tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
    Directory.CreateDirectory( tempDir );
    var baseName = "project";
    var specYaml = baseName + ".spec.yaml";
    var filePath = CreateTempFile( tempDir, specYaml );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), tempDir );
    var result = resolver.Resolve( baseName );

    Assert.That( result, Is.Not.Null );
    Assert.That( result.FullName, Is.EqualTo( filePath ) );

    Directory.Delete( tempDir, true );
  }

  [Test]
  public void Resolve_NoName_FindsDefaultSpecFile() {
    var tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
    Directory.CreateDirectory( tempDir );
    var specName = "drift.spec.yaml";
    var filePath = CreateTempFile( tempDir, specName );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), tempDir );
    var result = resolver.Resolve( null );

    Assert.That( result, Is.Not.Null );
    Assert.That( result.FullName, Is.EqualTo( filePath ) );

    Directory.Delete( tempDir, true );
  }

  [Test]
  public void Resolve_NotFound_ReturnsNull() {
    var tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
    Directory.CreateDirectory( tempDir );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), tempDir );
    var result = resolver.Resolve( "notfound.yaml" );

    Assert.That( result, Is.Null );

    Directory.Delete( tempDir, true );
  }

  [Test]
  public void Resolve_NotFoundWithThrows_ThrowsFileNotFoundException() {
    var tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
    Directory.CreateDirectory( tempDir );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), tempDir );

    Assert.Throws<FileNotFoundException>( () => resolver.Resolve( "notfound.yaml", true ) );

    Directory.Delete( tempDir, true );
  }

  [Test]
  public void Priority_ExactFileName_BeforeSpecYaml() {
    var tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
    Directory.CreateDirectory( tempDir );

    var name = "mything";
    var exact = CreateTempFile( tempDir, name );
    var spec = CreateTempFile( tempDir, name + ".spec.yaml" );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), tempDir );
    var result = resolver.Resolve( name );

    Assert.That( result, Is.Not.Null );
    Assert.That( result.FullName, Is.EqualTo( exact ) );
    Directory.Delete( tempDir, true );
  }

  [Test]
  public void Priority_NameSpecYaml_IfExactFileMissing() {
    var tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
    Directory.CreateDirectory( tempDir );

    var name = "someproject";
    var spec = CreateTempFile( tempDir, name + ".spec.yaml" );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), tempDir );
    var result = resolver.Resolve( name );

    Assert.That( result, Is.Not.Null );
    Assert.That( result.FullName, Is.EqualTo( spec ) );
    Directory.Delete( tempDir, true );
  }

  [Test]
  public void Priority_NameSpecYml_ExtensionHandled() {
    var tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
    Directory.CreateDirectory( tempDir );

    var name = "otherproject";
    var spec = CreateTempFile( tempDir, name + ".spec.yml" );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), tempDir );
    var result = resolver.Resolve( name );

    Assert.That( result, Is.Not.Null );
    Assert.That( result.FullName, Is.EqualTo( spec ) );
    Directory.Delete( tempDir, true );
  }

  [Test]
  public void Priority_DriftSpecYaml_OverWildcard() {
    var tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
    Directory.CreateDirectory( tempDir );

    var drift = CreateTempFile( tempDir, "drift.spec.yaml" );
    var other = CreateTempFile( tempDir, "other.spec.yaml" );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), tempDir );
    var result = resolver.Resolve( null );

    Assert.That( result, Is.Not.Null );
    Assert.That( result.FullName, Is.EqualTo( drift ) );
    Directory.Delete( tempDir, true );
  }

  [Test]
  public void Priority_WildcardUsed_IfNoDriftSpecYaml() {
    var tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
    Directory.CreateDirectory( tempDir );

    var one = CreateTempFile( tempDir, "first.spec.yaml" );
    //var two = CreateTempFile( tempDir, "second.spec.yaml" );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), tempDir );
    var result = resolver.Resolve( null );

    // Should be first discovered
    Assert.That( result, Is.Not.Null );
    Assert.That( result.FullName, Is.EqualTo( one ) );
    Directory.Delete( tempDir, true );
  }

  [Test]
  public void Priority_WildcardYamlPicked() {
    var tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
    Directory.CreateDirectory( tempDir );

    var yml = CreateTempFile( tempDir, "file.spec.yml" );
    var yaml = CreateTempFile( tempDir, "file.spec.yaml" );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), tempDir );
    var result = resolver.Resolve( null );

    // Should pick the first one (yml)
    Assert.That( result, Is.Not.Null );
    Assert.That( result.FullName, Is.EqualTo( yaml ) );
    Directory.Delete( tempDir, true );
  }

  [Test]
  public void EmptyName_ThrowsArgumentException() {
    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), Directory.GetCurrentDirectory() );
    var ex = Assert.Throws<ArgumentException>( () => resolver.Resolve( "" ) );
    Assert.That( ex.ParamName, Is.EqualTo( "name" ) );
  }

  [Test]
  public void TildeWithSeparator_ExpandsAndFindsFile() {
    var home = Environment.GetFolderPath( Environment.SpecialFolder.UserProfile );
    var fname = "myhome.yaml";
    var fpath = CreateTempFile( home, fname );

    var resolver = new SpecFilePathResolver( CreateOutputSubstitute(), Directory.GetCurrentDirectory() );
    var result = resolver.Resolve( "~" + Path.DirectorySeparatorChar + fname );

    Assert.That( result, Is.Not.Null );
    Assert.That( result.FullName, Is.EqualTo( Path.Combine( home, fname ) ) );
    File.Delete( fpath );
  }

  [Test]
  public void MultipleSpecFiles_LogsWarning() {
    var tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
    Directory.CreateDirectory( tempDir );

    var yaml = CreateTempFile( tempDir, "one.spec.yaml" );
    var yml = CreateTempFile( tempDir, "two.spec.yml" );

    var output = CreateOutputSubstitute();
    var resolver = new SpecFilePathResolver( output, tempDir );
    var result = resolver.Resolve( null );

    // Should still pick first
    Assert.That( result, Is.Not.Null );
    Assert.That( result.FullName, Is.EqualTo( yaml ).Or.EqualTo( yml ) );

    // Check warning was called
    output.Log.ReceivedWithAnyArgs().LogWarning( default, default, default, default, default );
    output.Normal.ReceivedWithAnyArgs().WriteLineWarning( "" );

    Directory.Delete( tempDir, true );
  }

  private static string CreateTempFile( string directory, string name ) {
    var path = Path.Combine( directory, name );
    File.WriteAllText( path, "dummy" );
    return path;
  }

  private static IOutputManager CreateOutputSubstitute() {
    var output = Substitute.For<IOutputManager>();
    output.Log.Returns( Substitute.For<ILogOutput>() );
    output.Normal.Returns( Substitute.For<INormalOutput>() );
    return output;
    //return new NullOutputManager();
  }
}