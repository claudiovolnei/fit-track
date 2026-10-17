using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.UseCases;

/// <summary>Edits an existing catalog exercise. Restricted to personal trainers.</summary>
public sealed class UpdateExercise : IUpdateExercise
{
    private readonly ICurrentUser _currentUser;
    private readonly IExerciseRepository _exercises;
    private readonly IMuscleGroupRepository _muscleGroups;

    public UpdateExercise(
        ICurrentUser currentUser,
        IExerciseRepository exercises,
        IMuscleGroupRepository muscleGroups)
    {
        _currentUser = currentUser;
        _exercises = exercises;
        _muscleGroups = muscleGroups;
    }

    public async Task<ExerciseDto> ExecuteAsync(UpdateExerciseCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsPersonalTrainer)
        {
            throw new ForbiddenException("Only personal trainers can manage the exercise catalog.");
        }

        var exercise = await _exercises.GetByIdAsync(new ExerciseId(command.ExerciseId), cancellationToken)
            ?? throw new NotFoundException($"Exercise '{command.ExerciseId}' was not found.");

        exercise.Rename(new ExerciseName(command.Name));
        exercise.ChangeMuscleGroup(new MuscleGroupId(command.MuscleGroupId));

        // The aggregate states the "no muscle group" rule, so it runs before the lookup:
        // empty id is a malformed request (400), an unknown id is a missing reference (404).
        var muscleGroup = await MuscleGroupLookup.RequireAsync(
            _muscleGroups,
            command.MuscleGroupId,
            cancellationToken);

        if (command.IsActive)
        {
            exercise.Activate();
        }
        else
        {
            exercise.Deactivate();
        }

        await _exercises.SaveAsync(exercise, cancellationToken);

        return exercise.ToDto(muscleGroup.Name);
    }
}
