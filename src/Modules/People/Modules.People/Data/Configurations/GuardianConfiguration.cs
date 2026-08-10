using FSH.Modules.People.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.People.Data.Configurations;

public sealed class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
{
    public void Configure(EntityTypeBuilder<Guardian> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Guardians");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LastName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.FirstName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(256);
        builder.Property(x => x.UserId).HasMaxLength(64);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.HasIndex(x => x.UserId).IsUnique().HasFilter("\"UserId\" IS NOT NULL AND \"IsDeleted\" = FALSE");
        builder.HasIndex(x => x.IsDeleted);

        builder.Ignore(x => x.DisplayName);
        builder.Ignore(x => x.DomainEvents);
    }
}
