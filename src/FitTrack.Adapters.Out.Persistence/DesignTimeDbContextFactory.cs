using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FitTrack.Adapters.Out.Persistence;

/// <summary>
/// Lets the EF tools build the model without booting the API host. The connection
/// string is only used to select the provider, never to open a connection.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FitTrackDbContext>
{
    public FitTrackDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FITTRACK_CONNECTION_STRING")
            ?? "Host=localhost;Database=fittrack;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<FitTrackDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new FitTrackDbContext(options);
    }
}
