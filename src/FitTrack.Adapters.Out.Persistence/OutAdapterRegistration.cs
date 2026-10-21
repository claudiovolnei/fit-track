using FitTrack.Adapters.Out.Persistence.Infrastructure;
using FitTrack.Adapters.Out.Persistence.Queries;
using FitTrack.Adapters.Out.Persistence.Repositories;
using FitTrack.Application.Ports.Out;
using Microsoft.Extensions.DependencyInjection;

namespace FitTrack.Adapters.Out.Persistence;

/// <summary>
/// Registers the outbound adapter implementations behind their application ports.
/// The <see cref="FitTrackDbContext"/> itself is added by the composition root, because the
/// adapter does not own the connection string; Aspire supplies it.
/// </summary>
public static class OutAdapterRegistration
{
    public static IServiceCollection AddOutAdapters(this IServiceCollection services)
    {
        services.AddScoped<IExerciseRepository, ExerciseRepository>();
        services.AddScoped<IMuscleGroupRepository, MuscleGroupRepository>();
        services.AddScoped<ICheckCatalogUsage, CatalogUsageQueries>();
        services.AddScoped<IWorkoutSessionRepository, WorkoutSessionRepository>();
        services.AddScoped<IWorkoutSeriesRepository, WorkoutSeriesRepository>();
        services.AddScoped<IWorkoutSeriesAssignmentRepository, WorkoutSeriesAssignmentRepository>();
        services.AddScoped<ITrainerAthleteAssignmentRepository, TrainerAthleteAssignmentRepository>();
        services.AddScoped<IQueryExercises, ExerciseQueries>();
        services.AddScoped<IQueryMuscleGroups, MuscleGroupQueries>();
        services.AddScoped<IQueryUserSessions, UserSessionQueries>();
        services.AddScoped<IQueryWorkoutSeries, WorkoutSeriesQueries>();
        services.AddScoped<IQueryAthletes, AthleteQueries>();
        services.AddScoped<IQueryUsers, UserQueries>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();

        return services;
    }
}
