using FitTrack.Application.Exceptions;
using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Application.Mapping;

/// <summary>
/// The level of intensity as the wire names it. The domain speaks of levels; the API speaks of the
/// words the athlete picks on the screen, and this is where the two meet — so the vocabulary lives in
/// one place instead of drifting between the client, the contract and the read model.
/// </summary>
public static class WorkoutIntensityMapping
{
    /// <summary>The word the level travels as. Null when the workout has no level.</summary>
    public static string? ToContract(WorkoutIntensity? intensity) =>
        intensity switch
        {
            WorkoutIntensity.Leve => "leve",
            WorkoutIntensity.Medio => "medio",
            WorkoutIntensity.Moderado => "moderado",
            WorkoutIntensity.Intenso => "intenso",
            WorkoutIntensity.MuitoIntenso => "muitoIntenso",
            null => null,
            _ => throw new InvalidCommandException("Unknown workout intensity."),
        };

    /// <summary>
    /// The level the athlete picked, read from the word that travelled. A word outside the list is
    /// refused with the rule that states the options.
    /// </summary>
    public static WorkoutIntensity FromContract(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "leve" => WorkoutIntensity.Leve,
            "medio" => WorkoutIntensity.Medio,
            "moderado" => WorkoutIntensity.Moderado,
            "intenso" => WorkoutIntensity.Intenso,
            "muitointenso" => WorkoutIntensity.MuitoIntenso,
            _ => throw new DomainException(
                "session.finish.intensity.invalid",
                "Choose the intensity of the workout: leve, medio, moderado, intenso or muitoIntenso."),
        };
}
