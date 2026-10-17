using FitTrack.UI.Auth;
using FitTrack.UI.Setup;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<FitTrack.UI.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080";

builder.Services.AddFitTrackUi(new Uri(apiBaseUrl));
builder.Services.AddScoped<ITokenStore, LocalStorageTokenStore>();

await builder.Build().RunAsync();
