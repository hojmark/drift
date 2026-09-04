using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Drift.Coordinator.Host.Ui;

internal static class EndpointRouteBuilderExtensions {
  private static readonly string Version = Assembly.GetEntryAssembly()?
                                             .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                             .InformationalVersion
                                           ?? throw new Exception( "Could not determine Drift version" );

  extension( IEndpointRouteBuilder endpoints ) {
    public void MapUi() {
      endpoints.MapGet( "/", () =>
        // TODO Render figlet using same flf as in the help command
        $"""
          ___          _    __   _
         |   \   _ _  (_)  / _| | |_
         | |) | | '_| | | |  _| |  _|
         |___/  |_|   |_| |_|    \__|
         Server

         Version: {Version}
         API docs: /api
         """
      ).ExcludeFromDescription();
    }
  }
}