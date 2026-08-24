using FSH.Modules.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Scheduling.Data.Configurations;

public sealed class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Attendances");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Comment).HasMaxLength(1024);
        builder.Property(x => x.MarkedByUserId).HasMaxLength(64);

        // One attendance row per student per session — seeded once on Session.Hold, never duplicated
        // (see docs/02 Модули/Scheduling.md → Инварианты).
        builder.HasIndex(x => new { x.SessionId, x.StudentId }).IsUnique();
        builder.HasIndex(x => x.StudentId); // GetStudentAttendanceQuery — cross-session history
        builder.HasIndex(x => x.Status);
    }
}
