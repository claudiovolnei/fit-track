using System.Net.Http.Json;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;
using FitTrack.Integration.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FitTrack.Integration;

/// <summary>
/// The arrangement the series suites share: the seeded trainer, a fresh account holding the trainer
/// profile, a catalog exercise to prescribe, and a series assigned to an athlete. Each step goes through
/// the API, so the suites arrange over the same edge the screens use.
/// </summary>
internal static class SeriesArrange
{
    /// <summary>The name the seed gives the trainer, which the athlete sees on the series screen.</summary>
    public const string TrainerName = "Claudio Volnei";

    public const string TrainerEmail = "trainer@fittrack.local";

    public const string TrainerPassword = "Trainer123$";

    public const string AdminEmail = "admin@fittrack.local";

    public const string AdminPassword = "Admin123$";

    /// <summary>The seeded personal trainer, signed in.</summary>
    public static async Task<HttpClient> TrainerAsync(this FitTrackApiFactory factory) =>
        await factory.CreateClient().SignInAsync(TrainerEmail, TrainerPassword);

    /// <summary>
    /// A fresh account holding the trainer profile, signed in afterwards so its token carries the
    /// profile. The administrator is the only one who can hand the profile out.
    /// </summary>
    public static async Task<HttpClient> NewTrainerAsync(
        this FitTrackApiFactory factory,
        string name = "Personal de Teste")
    {
        var email = $"trainer-{Guid.NewGuid():N}@fittrack.local";
        var admin = await factory.CreateClient().SignInAsync(AdminEmail, AdminPassword);

        var created = await admin.PostAsJsonAsync(
            "/admin/users",
            new { name, email, password = TrainerPassword, roles = new[] { "PersonalTrainer" } });

        await created.EnsureSuccessWithBodyAsync();

        return await factory.CreateClient().SignInAsync(email, TrainerPassword);
    }

    /// <summary>A catalog exercise created by the trainer, so a prescription has something to point at.</summary>
    public static async Task<ExerciseResponse> NewExerciseAsync(HttpClient trainer, string prefix = "Série")
    {
        var muscleGroup = await trainer.CreateMuscleGroupAsync(prefix);

        var created = await trainer.PostAsJsonAsync(
            "/exercises",
            new CreateExerciseRequest($"{prefix} {Guid.NewGuid():N}", muscleGroup.Id));

        await created.EnsureSuccessWithBodyAsync();

        return (await created.Content.ReadFromJsonAsync<ExerciseResponse>())!;
    }

    /// <summary>
    /// A series with one workout, assigned to the athlete, all of it through the API. Gives back the
    /// workout the athlete can start.
    /// </summary>
    public static async Task<Guid> AssignSeriesAsync(
        HttpClient trainer,
        Guid athleteId,
        params PrescribedExerciseRequest[] prescription)
    {
        var series = await trainer.CreateSeriesAsync($"Série {Guid.NewGuid():N}", athleteId);
        var workout = await trainer.AddWorkoutAsync(series.Id, "Peito e Tríceps", prescription);

        return workout.Id;
    }

    /// <summary>
    /// Moves the assignment's start back so the sixty days are over. The write side recomputes the term
    /// from <c>assigned_at</c> on every read, so writing only the expires_at column would leave the API
    /// still treating the series as valid — which is why this goes through the aggregate.
    /// </summary>
    public static async Task ExpireSeriesAsync(this FitTrackApiFactory factory, Guid athleteId)
    {
        using var scope = factory.Services.CreateScope();

        var assignments = scope.ServiceProvider.GetRequiredService<IWorkoutSeriesAssignmentRepository>();
        var userId = new UserId(athleteId);
        var current = await assignments.GetByUserIdAsync(userId, CancellationToken.None);

        await assignments.SaveAsync(
            WorkoutSeriesAssignment.Assign(
                current!.Id,
                current.SeriesId,
                current.UserId,
                current.TrainerId,
                DateTimeOffset.UtcNow.AddDays(-(WorkoutSeriesAssignment.TermInDays + 1))),
            CancellationToken.None);
    }
}
