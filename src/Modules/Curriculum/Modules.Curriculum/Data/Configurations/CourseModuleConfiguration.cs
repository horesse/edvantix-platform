using FSH.Modules.Curriculum.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Curriculum.Data.Configurations;

public sealed class CourseModuleConfiguration : IEntityTypeConfiguration<CourseModule>
{
    public void Configure(EntityTypeBuilder<CourseModule> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("CourseModules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CourseId).IsRequired();
        builder.HasIndex(x => x.CourseId);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.SortOrder).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
