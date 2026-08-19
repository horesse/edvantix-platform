using FSH.Modules.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Scheduling.Data.Configurations;

public sealed class NonWorkingDayConfiguration : IEntityTypeConfiguration<NonWorkingDay>
{
    public void Configure(EntityTypeBuilder<NonWorkingDay> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("NonWorkingDays");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).HasMaxLength(256);

        // One row per date per tenant — the generator queries this by date range, and the API
        // shouldn't let a manager create the same holiday twice.
        builder.HasIndex(x => x.Date).IsUnique();
    }
}
