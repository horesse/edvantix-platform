using FSH.Modules.StudyGroups.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.StudyGroups.Data.Configurations;

public sealed class GroupTeacherConfiguration : IEntityTypeConfiguration<GroupTeacher>
{
    public void Configure(EntityTypeBuilder<GroupTeacher> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("GroupTeachers");
        builder.HasKey(x => x.Id);

        // Same ValueGeneratedNever reasoning as GroupEnrollmentConfiguration.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.StudyGroupId).IsRequired();
        builder.Property(x => x.TeacherId).IsRequired();
        builder.Property(x => x.Role).IsRequired().HasConversion<string>().HasMaxLength(16);

        // A teacher holds at most one role at a time on a given group's roster (StudyGroup.AddTeacher
        // rejects duplicates) — hard-deleted, so a plain unique index (no soft-delete filter needed).
        builder.HasIndex(x => new { x.StudyGroupId, x.TeacherId }).IsUnique();
        builder.HasIndex(x => x.TeacherId);
    }
}
