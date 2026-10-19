namespace FitTrack.Integration.Contracts;

/// <summary>
/// Request body for naming a series. Creating and renaming share it, because a series is born with
/// nothing but a name.
/// </summary>
public sealed record SetWorkoutSeriesNameRequest(string Name);

/// <summary>
/// Request body for one prescribed exercise. The load is nullable because a bodyweight exercise has no
/// weight to prescribe.
/// </summary>
public sealed record PrescribedExerciseRequest(Guid ExerciseId, int Sets, int Repetitions, decimal? LoadKilograms);

/// <summary>Request body for the name and the prescription of one workout of a series.</summary>
public sealed record SetSeriesWorkoutRequest(string Name, IReadOnlyList<PrescribedExerciseRequest> Exercises);

/// <summary>Request body for putting a series in the hands of one athlete.</summary>
public sealed record AssignWorkoutSeriesRequest(Guid UserId);
