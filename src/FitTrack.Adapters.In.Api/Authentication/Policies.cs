namespace FitTrack.Adapters.In.Api.Authentication;

/// <summary>Authorization policy names used by the endpoints.</summary>
internal static class Policies
{
    public const string AdminOnly = "AdminOnly";

    /// <summary>Authoring the catalog and the training plans belongs to the personal trainer.</summary>
    public const string PersonalTrainerOnly = "PersonalTrainerOnly";

    /// <summary>The roster screens both profiles share: linking and listing athlete access.</summary>
    public const string TrainerOrAdmin = "TrainerOrAdmin";
}
