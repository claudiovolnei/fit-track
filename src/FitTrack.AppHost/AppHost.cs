var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    // Named volume so the development database survives an AppHost restart.
    // Without it DCP recreates the container with a fresh anonymous volume on every run.
    .WithDataVolume("fittrack-postgres-data");
var database = postgres.AddDatabase("fittrack");

var api = builder.AddProject<Projects.FitTrack_Adapters_In_Api>("api")
    // Fixed port so the Blazor WebAssembly client, which is served as static files,
    // can reach the API without runtime service discovery.
    .WithHttpEndpoint(port: 5080, name: "http")
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<Projects.FitTrack_Web>("web")
    .WithHttpEndpoint(port: 5090, name: "http")
    .WithReference(api)
    .WaitFor(api);

await builder.Build().RunAsync();
