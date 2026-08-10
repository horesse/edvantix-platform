using FSH.Modules.Curriculum.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Curriculum.Data.Configurations;

public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Lessons");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CourseModuleId).IsRequired();
        builder.HasIndex(x => x.CourseModuleId);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Objectives).HasMaxLength(2000);
        builder.Property(x => x.Content).HasMaxLength(20000);
        builder.Property(x => x.DurationMinutes).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
