using FitTrack.Adapters.Out.Persistence.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitTrack.Adapters.Out.Persistence.Configuration;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        // The release that lets an athlete build their own plan. It defaults to false so every
        // account that exists today keeps the personal trainer as the only way to a plan.
        builder.Property(account => account.CanAuthorOwnWorkout)
            .HasDefaultValue(false)
            .IsRequired();
    }
}
