using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Multitenancy.Data.Configurations;

public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TenantSettings", MultitenancyConstants.Schema);

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.TenantId)
            .IsUnique();

        builder.Property(s => s.TenantId)
            .HasMaxLength(64)
            .IsRequired();

        // Longest IANA identifiers (e.g. "America/Argentina/ComodRivadavia") sit around 32 chars.
        builder.Property(s => s.TimeZoneId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(s => s.Currency)
            .HasMaxLength(3)
            .IsRequired();

        // Audit
        builder.Property(s => s.CreatedOnUtc).IsRequired();
        builder.Property(s => s.CreatedBy).HasMaxLength(256);
        builder.Property(s => s.LastModifiedBy).HasMaxLength(256);
    }
}
