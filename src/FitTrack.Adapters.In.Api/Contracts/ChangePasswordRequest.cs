namespace FitTrack.Adapters.In.Api.Contracts;

/// <summary>The current password and the new one, for a self-service password change.</summary>
internal sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
