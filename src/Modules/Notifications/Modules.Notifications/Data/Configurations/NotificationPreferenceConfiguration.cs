using FSH.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Notifications.Data.Configurations;

internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(64).IsRequired();
        builder.Property(x => x.InAppEnabled).IsRequired();
        builder.Property(x => x.EmailEnabled).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        // One row per (user, type); the effective-preferences read is `WHERE UserId=?`.
        builder.HasIndex(x => new { x.UserId, x.Type }).IsUnique();

        builder.Ignore(x => x.DomainEvents);
    }
}
