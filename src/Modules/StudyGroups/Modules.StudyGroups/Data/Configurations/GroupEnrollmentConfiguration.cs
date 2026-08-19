using FSH.Modules.StudyGroups.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.StudyGroups.Data.Configurations;

public sealed class GroupEnrollmentConfiguration : IEntityTypeConfiguration<GroupEnrollment>
{
    public void Configure(EntityTypeBuilder<GroupEnrollment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("GroupEnrollments");
        builder.HasKey(x => x.Id);

        // Id is app-assigned (Guid.CreateVersion7). Without ValueGeneratedNever, EF treats the
        // non-default Guid reached via the tracked StudyGroup's nav collection as persisted →
        // UPDATE-0-rows (same footgun as People.StudentGuardian).
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.StudyGroupId).IsRequired();
        builder.Property(x => x.StudentId).IsRequired();
        builder.Property(x => x.EnrolledOn).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.LeaveReason).HasMaxLength(512);
        builder.Property(x => x.DiscountPercent).HasPrecision(5, 2);

        // "One active enrollment per student per group" is enforced in StudyGroup.Enroll, not a DB
        // constraint — re-enrollment after Left must be allowed, and a partial unique index scoped
        // to "Status <> Left" can't distinguish Active from Paused from Completed as cleanly as the
        // aggregate check (same reasoning as People's single-primary-payer).
        builder.HasIndex(x => new { x.StudyGroupId, x.StudentId });
        builder.HasIndex(x => x.StudentId); // GetStudentEnrollmentsQuery — cross-group lookup
        builder.HasIndex(x => x.Status);
    }
}
