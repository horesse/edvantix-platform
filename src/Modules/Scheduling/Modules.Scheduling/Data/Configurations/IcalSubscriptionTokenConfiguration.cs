using FSH.Modules.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Scheduling.Data.Configurations;

public sealed class IcalSubscriptionTokenConfiguration : IEntityTypeConfiguration<IcalSubscriptionToken>
{
    public void Configure(EntityTypeBuilder<IcalSubscriptionToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("IcalSubscriptionTokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token).HasMaxLength(64).IsRequired();

        // One feed per user per tenant; the .ics endpoint resolves the owner by token. Both indexes
        // are folded to (TenantId, …) by the tenant-isolation model builder.
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.Token).IsUnique();
    }
}
