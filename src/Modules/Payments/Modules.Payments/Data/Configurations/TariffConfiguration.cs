using FSH.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Payments.Data.Configurations;

public sealed class TariffConfiguration : IEntityTypeConfiguration<Tariff>
{
    public void Configure(EntityTypeBuilder<Tariff> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Tariffs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Kind).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(8);

        builder.HasIndex(x => x.CourseId);
        builder.HasIndex(x => x.IsActive);

        builder.Ignore(x => x.DomainEvents);
    }
}
