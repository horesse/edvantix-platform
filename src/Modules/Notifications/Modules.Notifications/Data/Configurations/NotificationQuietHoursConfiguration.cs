using FSH.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Notifications.Data.Configurations;

internal sealed class NotificationQuietHoursConfiguration : IEntityTypeConfiguration<NotificationQuietHours>
{
    public void Configure(EntityTypeBuilder<NotificationQuietHours> builder)
    {
        builder.ToTable("NotificationQuietHours");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Enabled).IsRequired();
        builder.Property(x => x.StartLocal).IsRequired();
        builder.Property(x => x.EndLocal).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
