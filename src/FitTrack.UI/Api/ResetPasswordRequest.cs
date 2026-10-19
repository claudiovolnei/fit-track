namespace FitTrack.UI.Api;

/// <summary>Wire contract: the new password an administrator sets for an account.</summary>
public sealed record ResetPasswordRequest(string NewPassword);
