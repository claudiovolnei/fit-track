using FitTrack.Adapters.Out.Persistence;
using FitTrack.Adapters.Out.Persistence.Accounts;
using FitTrack.Adapters.Out.Persistence.Records;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.In.Api.Seeding;

/// <summary>
/// Development-only accounts and roles, so the acceptance flow can run without manual setup.
/// Never invoked outside the Development environment.
/// </summary>
internal static class DevelopmentSeeder
{
    public const string AdminEmail = "admin@fittrack.local";

    public const string AdminPassword = "Admin123$";

    public const string TrainerEmail = "trainer@fittrack.local";

    public const string TrainerPassword = "Trainer123$";

    public const string AthleteEmail = "athlete@fittrack.local";

    public const string AthletePassword = "Athlete123$";

    public const string Athlete2Email = "athlete2@fittrack.local";

    public const string Athlete2Password = "Athlete123$";

    public static async Task SeedAsync(IServiceProvider services)
    {
        await RoleSeeder.EnsureRolesAsync(services);

        var users = services.GetRequiredService<UserManager<AppUser>>();
        await EnsureAccountAsync(users, AdminEmail, "Administrador", AdminPassword, Roles.Admin);
        var trainer = await EnsureAccountAsync(users, TrainerEmail, "Claudio Volnei", TrainerPassword, Roles.PersonalTrainer);
        var athlete = await EnsureAccountAsync(users, AthleteEmail, "Atleta FitTrack", AthletePassword, Roles.User);

        // A second athlete with no trainer at all, so the link screen (and its candidate search)
        // has both states ready: one athlete on the trainer's roster and one on nobody's.
        await EnsureAccountAsync(users, Athlete2Email, "Atleta Nômade", Athlete2Password, Roles.User);

        await EnsureRosterLinkAsync(services, trainer, athlete);
        await EnsureCatalogAsync(services.GetRequiredService<FitTrackDbContext>());
    }

    /// <summary>
    /// The seeded athlete answers to the seeded trainer, so the acceptance flow (train, follow,
    /// prescribe) runs without setting the roster by hand. Written through the repository on purpose:
    /// the seed has no authenticated caller, but the gather rules stay the domain's.
    /// </summary>
    private static async Task EnsureRosterLinkAsync(
        IServiceProvider services,
        AppUser trainer,
        AppUser athlete)
    {
        if (trainer is null || athlete is null)
        {
            return;
        }

        var links = services.GetRequiredService<ITrainerAthleteAssignmentRepository>();
        var athleteId = new UserId(athlete.Id);

        if (await links.GetByAthleteIdAsync(athleteId, CancellationToken.None) is not null)
        {
            return;
        }

        await links.SaveAsync(
            TrainerAthleteAssignment.Link(
                new TrainerAthleteAssignmentId(Guid.NewGuid()),
                new UserId(trainer.Id),
                athleteId,
                DateTimeOffset.UtcNow),
            CancellationToken.None);
    }

    /// <summary>
    /// Starter catalog so the trainer does not face an empty screen. Written straight through the
    /// context on purpose: the seed has no authenticated caller to go through the use cases.
    /// </summary>
    private static async Task EnsureCatalogAsync(FitTrackDbContext dbContext)
    {
        (string MuscleGroup, string[] Exercises)[] catalog =
        [
            ("Chest", ["Bench Press", "Incline Bench Press"]),
            ("Back", ["Barbell Row", "Lat Pulldown"]),
            ("Legs", ["Back Squat", "Leg Press"]),
            ("Biceps", ["Barbell Curl"]),
            ("Triceps", ["Triceps Pushdown"]),
        ];

        foreach (var (muscleGroupName, exerciseNames) in catalog)
        {
            var muscleGroup = await dbContext.MuscleGroups
                .FirstOrDefaultAsync(candidate => candidate.Name == muscleGroupName);

            if (muscleGroup is null)
            {
                muscleGroup = new MuscleGroupRecord { Id = Guid.NewGuid(), Name = muscleGroupName };
                dbContext.MuscleGroups.Add(muscleGroup);
            }

            foreach (var exerciseName in exerciseNames)
            {
                if (await dbContext.Exercises.AnyAsync(exercise => exercise.Name == exerciseName))
                {
                    continue;
                }

                dbContext.Exercises.Add(new ExerciseRecord
                {
                    Id = Guid.NewGuid(),
                    Name = exerciseName,
                    MuscleGroupId = muscleGroup.Id,
                    IsActive = true,
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<AppUser> EnsureAccountAsync(
        UserManager<AppUser> users,
        string email,
        string name,
        string password,
        string role)
    {
        var account = await users.FindByEmailAsync(email);
        if (account is not null)
        {
            // The column arrived after these accounts existed; fill it so the screens show a name.
            if (string.IsNullOrWhiteSpace(account.Name))
            {
                account.Name = name;
                await users.UpdateAsync(account);
            }

            return account;
        }

        account = new AppUser
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        var created = await users.CreateAsync(account, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not seed the development account '{email}': " +
                string.Join("; ", created.Errors.Select(error => error.Description)));
        }

        await users.AddToRoleAsync(account, role);

        return account;
    }
}
