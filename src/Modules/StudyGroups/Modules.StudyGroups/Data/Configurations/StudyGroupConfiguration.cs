using FSH.Modules.StudyGroups.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.StudyGroups.Data.Configurations;

public sealed class StudyGroupConfiguration : IEntityTypeConfiguration<StudyGroup>
{
    public void Configure(EntityTypeBuilder<StudyGroup> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("StudyGroups");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Format).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.MeetingUrl).HasMaxLength(512);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        // Tenant isolation scopes this per-tenant (same physical-schema-per-tenant model documented
        // on Student.UserId) — a plain unique index is enough, no need to fold TenantId in.
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(x => x.CourseId);
        builder.HasIndex(x => x.PrimaryTeacherId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.IsDeleted);

        // Not auto-included — SearchStudyGroups lists shouldn't drag in every enrollment/teacher row.
        // GetStudyGroupByIdQuery/GetGroupEnrollmentsQuery Include explicitly.
        builder.HasMany(x => x.Enrollments)
            .WithOne()
            .HasForeignKey(e => e.StudyGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Teachers)
            .WithOne()
            .HasForeignKey(t => t.StudyGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.ActiveEnrollmentCount);
        builder.Ignore(x => x.DomainEvents);
    }
}
