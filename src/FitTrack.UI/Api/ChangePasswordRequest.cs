namespace FitTrack.UI.Api;

/// <summary>Wire contract: the current password and the new one, for a self-service change.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
