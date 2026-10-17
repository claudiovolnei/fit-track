namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>HTTP contract for signing in.</summary>
internal sealed record LoginRequest(string Email, string Password);
