namespace FitTrack.UI.Auth;

/// <summary>Stores the access token. The host decides where: browser storage or device secure storage.</summary>
public interface ITokenStore
{
    ValueTask<string?> GetAsync();

    ValueTask SaveAsync(string token);

    ValueTask ClearAsync();
}
