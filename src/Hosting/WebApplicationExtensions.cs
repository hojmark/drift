using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Drift.Hosting;

public static class WebApplicationExtensions {
  // Request paths are logged in their encoded form using ToUriComponent(), so decoded control characters such as line breaks (e.g. from %0A)
  // can't be used to forge log entries.
  extension( WebApplication app ) {
    public void AddRequestLogging( ILogger logger ) {
      app.Use( async ( context, next ) => {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
          "HTTP request started  : {RequestId} {Method} {Path}",
          context.TraceIdentifier,
          context.Request.Method,
          context.Request.Path.ToUriComponent()
        );

        try {
          await next();
        }
        finally {
          stopwatch.Stop();
          logger.LogInformation(
            "HTTP request completed: {RequestId} {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds}ms",
            context.TraceIdentifier,
            context.Request.Method,
            context.Request.Path.ToUriComponent(),
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds
          );
        }
      } );
    }

    public void AddGlobalExceptionHandling( ILogger logger ) {
      app.UseExceptionHandler( errorApp => errorApp.Run( async context => {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        logger.LogError(
          exception,
          "Unhandled HTTP request failure for {Path}",
          context.Request.Path.ToUriComponent()
        );
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync( exception?.ToString() ?? "Unknown request failure" );
      } ) );
    }
  }
}