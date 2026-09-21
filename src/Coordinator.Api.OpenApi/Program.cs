using Drift.Coordinator.Api;
using Microsoft.AspNetCore.Builder;

// Dummy web application to support generation of OpenAPI spec

var builder = WebApplication.CreateSlimBuilder( args );
builder.Services.AddCoordinatorApi();

var app = builder.Build();
app.MapCoordinatorApi();

await app.RunAsync();