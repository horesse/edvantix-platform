using FSH.Modules.Curriculum.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Curriculum.Data.Configurations;

public sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Subjects");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(160);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.ParentId);
        builder.Property(x => x.SortOrder).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
