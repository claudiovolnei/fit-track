namespace FitTrack.Domain.Identity;

/// <summary>Role names shared by the Identity context and the authorization policies.</summary>
public static class Roles
{
    public const string Admin = "Admin";

    /// <summary>Profile allowed to author the catalog and the training plans.</summary>
    public const string PersonalTrainer = "PersonalTrainer";

    public const string User = "User";

    /// <summary>Every known role, in seeding order.</summary>
    public static IReadOnlyList<string> All { get; } = [Admin, PersonalTrainer, User];
}
