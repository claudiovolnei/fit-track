namespace FitTrack.Integration;

/// <summary>
/// A freshly registered athlete: the id the API assigned, the e-mail, and a signed-in client, so a test
/// can act as the athlete and as the trainer over the same account without guessing an id.
/// </summary>
internal sealed record AthleteHandle(Guid UserId, string Email, HttpClient Client)
{
    private const string Password = "Athlete123$";

    public static async Task<AthleteHandle> CreateAsync(FitTrackApiFactory factory)
    {
        var email = $"athlete-{Guid.NewGuid():N}@fittrack.local";
        var client = factory.CreateClient();
        var userId = await client.RegisterAccountAsync(email, Password);

        await client.SignInAsync(email, Password);

        return new AthleteHandle(userId, email, client);
    }

    /// <summary>Signs a new client in as this athlete and gives it back.</summary>
    public async Task<HttpClient> SignInAsync(FitTrackApiFactory factory) =>
        await factory.CreateClient().SignInAsync(Email, Password);
}

internal static class AthleteFactoryExtensions
{
    public static Task<AthleteHandle> CreateAthleteAsync(this FitTrackApiFactory factory) =>
        AthleteHandle.CreateAsync(factory);
}
