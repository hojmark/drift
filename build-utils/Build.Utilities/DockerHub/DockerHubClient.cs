using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using HLabs.ImageReferences;

namespace Drift.Build.Utilities.DockerHub;

public sealed class DockerHubClient( string? dockerHubUsername, string? dockerHubPassword ) : IDisposable {
  private const uint PageSize = 100;

#pragma warning disable S1075
  private readonly HttpClient _http = new() { BaseAddress = new Uri( "https://hub.docker.com" ) };
#pragma warning restore S1075


  public async Task<List<Tag>> ListTags( ImageRef imageRef ) {
    ArgumentNullException.ThrowIfNull( imageRef );
    ArgumentNullException.ThrowIfNull( imageRef.Namespace );
    ArgumentNullException.ThrowIfNull( imageRef.Repository );

    var tagsString = await ListTags( imageRef.Namespace.ToString(), imageRef.Repository.ToString() );

    return tagsString.Select( t => new Tag( t ) ).ToList();
  }

  private async Task<List<string>> ListTags( string @namespace, string repository ) {
    await EnsureLoggedIn();

    var tagsResponse = await _http.GetAsync( $"/v2/repositories/{@namespace}/{repository}/tags/?page_size={PageSize}" );
    tagsResponse.EnsureSuccessStatusCode();
    var tagsJson = JsonNode.Parse( await tagsResponse.Content.ReadAsStringAsync() );
    var tags = tagsJson!["results"]!
      .AsArray()
      .Select( t => t!["name"]!.GetValue<string>() )
      .ToList();

    if ( tags.Count >= PageSize ) {
      throw new NotImplementedException( $"Handling {PageSize} or more tags not supported" );
    }

    return tags;
  }

  /// <summary>
  /// Tag-only, image/manifest is left untouched.
  /// </summary>
  public async Task DeleteTag( ImageRef imageRef ) {
    ArgumentNullException.ThrowIfNull( imageRef );
    ArgumentNullException.ThrowIfNull( imageRef.Namespace );
    ArgumentNullException.ThrowIfNull( imageRef.Repository );
    ArgumentNullException.ThrowIfNull( imageRef.Tag );

    await DeleteTag( imageRef.Namespace.ToString(), imageRef.Repository.ToString(), imageRef.Tag.ToString() );
  }

  private async Task DeleteTag( string @namespace, string repository, string tag ) {
    await EnsureLoggedIn();

    var response = await _http.DeleteAsync( $"/v2/repositories/{@namespace}/{repository}/tags/{tag}/" );
    response.EnsureSuccessStatusCode();
  }

  private async Task EnsureLoggedIn() {
    if ( _http.DefaultRequestHeaders.Authorization == null ) {
      await Login();
    }
  }

  private async Task Login() {
    var loginBody = JsonNode.Parse( "{}" )!.AsObject();
    loginBody["username"] = dockerHubUsername;
    loginBody["password"] = dockerHubPassword;
    var loginResponse = await _http.PostAsync(
      "/v2/users/login",
      new StringContent( loginBody.ToJsonString(), Encoding.UTF8, "application/json" )
    );
    loginResponse.EnsureSuccessStatusCode();
    var loginJson = JsonNode.Parse( await loginResponse.Content.ReadAsStringAsync() );
    var token = loginJson!["token"]!.GetValue<string>();
    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", token );
  }

  public void Dispose() {
    _http.Dispose();
  }
}