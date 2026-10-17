namespace FitTrack.Adapters.In.Api.Composition;

/// <summary>
/// Browser clients run on their own origin (the WASM host, and later the MAUI WebView),
/// so the API has to state which origins are allowed to call it.
/// </summary>
internal sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>Origins allowed to call the API from a browser.</summary>
    public string[] AllowedOrigins { get; set; } = ["http://localhost:5090", "https://localhost:7278"];
}
