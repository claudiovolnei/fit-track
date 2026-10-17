using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FitTrack.Integration;

/// <summary>
/// Boots the real API in process against a real PostgreSQL database. The API applies its
/// migrations and the development seed on startup, exactly as it does when the AppHost runs it.
/// </summary>
public sealed class FitTrackApiFactory : WebApplicationFactory<Program>
{
    public FitTrackApiFactory()
    {
        var connectionString = Environment.GetEnvironmentVariable(RequiresPostgresFactAttribute.ConnectionStringVariable);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__fittrack", connectionString);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");
}
