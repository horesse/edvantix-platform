using FSH.Modules.Curriculum.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Curriculum.Data.Configurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Courses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SubjectId).IsRequired();
        builder.HasIndex(x => x.SubjectId);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(220);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Level).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.DurationHours).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.HasIndex(x => x.IsDeleted);

        builder.Ignore(x => x.DomainEvents);
    }
}
