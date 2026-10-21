using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Removes an exercise from the catalog. Restricted to personal trainers and refused while any
/// prescription or performed set references it: the workout history must not lose its exercise.
/// </summary>
public sealed class DeleteExercise : IDeleteExercise
{
    private readonly ICurrentUser _currentUser;
    private readonly IExerciseRepository _exercises;
    private readonly ICheckCatalogUsage _usage;

    public DeleteExercise(ICurrentUser currentUser, IExerciseRepository exercises, ICheckCatalogUsage usage)
    {
        _currentUser = currentUser;
        _exercises = exercises;
        _usage = usage;
    }

    public async Task ExecuteAsync(DeleteExerciseCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsPersonalTrainer)
        {
            throw new ForbiddenException("Only personal trainers can manage the exercise catalog.");
        }

        var exercise = await _exercises.GetByIdAsync(new ExerciseId(command.ExerciseId), cancellationToken)
            ?? throw new NotFoundException($"Exercise '{command.ExerciseId}' was not found.");

        if (await _usage.ExerciseReferencedAsync(exercise.Id, cancellationToken))
        {
            throw new ConflictException(
                "exercise.in_use",
                $"Exercise '{exercise.Name.Value}' is referenced by a prescription or a performed set.");
        }

        await _exercises.RemoveAsync(exercise, cancellationToken);
    }
}
