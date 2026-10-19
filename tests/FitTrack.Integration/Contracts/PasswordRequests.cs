namespace FitTrack.Integration.Contracts;

/// <summary>The current password and the new one, for a self-service change.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>The new password an administrator sets for an account.</summary>
public sealed record ResetPasswordRequest(string NewPassword);
