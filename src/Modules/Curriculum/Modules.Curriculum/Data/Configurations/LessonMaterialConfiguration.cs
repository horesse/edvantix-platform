using FSH.Modules.Curriculum.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Curriculum.Data.Configurations;

public sealed class LessonMaterialConfiguration : IEntityTypeConfiguration<LessonMaterial>
{
    public void Configure(EntityTypeBuilder<LessonMaterial> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("LessonMaterials", t => t.HasCheckConstraint(
            "CK_LessonMaterials_FileXorUrl",
            "(\"FileId\" IS NOT NULL AND \"Url\" IS NULL) OR (\"FileId\" IS NULL AND \"Url\" IS NOT NULL)"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LessonId).IsRequired();
        builder.HasIndex(x => x.LessonId);

        builder.Property(x => x.Kind).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.FileId);
        builder.Property(x => x.Url).HasMaxLength(2048);
        builder.Property(x => x.VisibleToStudents).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
