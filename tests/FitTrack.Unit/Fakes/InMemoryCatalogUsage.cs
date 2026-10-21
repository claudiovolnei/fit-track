using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Unit.Fakes;

/// <summary>
/// In-memory usage report for catalog deletion. The test drives what the catalog returns;
/// <see cref="ReportedExerciseCount"/> keeps the refusal observable when it matters.
/// </summary>
internal sealed class InMemoryCatalogUsage : ICheckCatalogUsage
{
    public bool ExerciseReferenced { get; set; }

    public bool MuscleGroupHasExercises { get; set; }

    public int Checks { get; private set; }

    public Task<bool> ExerciseReferencedAsync(ExerciseId exerciseId, CancellationToken cancellationToken)
    {
        Checks++;

        return Task.FromResult(ExerciseReferenced);
    }

    public Task<bool> MuscleGroupHasExercisesAsync(MuscleGroupId muscleGroupId, CancellationToken cancellationToken)
    {
        Checks++;

        return Task.FromResult(MuscleGroupHasExercises);
    }
}
