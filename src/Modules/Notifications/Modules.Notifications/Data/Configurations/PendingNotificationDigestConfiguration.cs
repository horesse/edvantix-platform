using FSH.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Notifications.Data.Configurations;

internal sealed class PendingNotificationDigestConfiguration : IEntityTypeConfiguration<PendingNotificationDigest>
{
    public void Configure(EntityTypeBuilder<PendingNotificationDigest> builder)
    {
        builder.ToTable("PendingNotificationDigests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.RecipientUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RecipientEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(1024);
        builder.Property(x => x.Link).HasMaxLength(512);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.SentAtUtc);

        // The flush job scans `WHERE SentAtUtc IS NULL ORDER BY CreatedAtUtc` then groups by e-mail.
        builder.HasIndex(x => new { x.SentAtUtc, x.CreatedAtUtc });

        builder.Ignore(x => x.DomainEvents);
    }
}
