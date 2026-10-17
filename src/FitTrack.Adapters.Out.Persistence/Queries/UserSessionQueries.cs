using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Adapters.Out.Persistence.Queries;

internal sealed class UserSessionQueries(FitTrackDbContext dbContext, IClock clock) : IQueryUserSessions
{
    public async Task<IReadOnlyList<WorkoutSessionDto>> ListByUserAsync(
        UserId userId,
        CancellationToken cancellationToken) =>
        await dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId.Value)
            .ListDtosAsync(clock.UtcNow, cancellationToken);
}
