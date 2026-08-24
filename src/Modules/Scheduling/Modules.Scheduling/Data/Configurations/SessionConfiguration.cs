using FSH.Modules.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Scheduling.Data.Configurations;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Sessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Topic).HasMaxLength(256);
        builder.Property(x => x.MeetingUrl).HasMaxLength(512);
        builder.Property(x => x.CancelReason).HasMaxLength(512);
        builder.Property(x => x.TeacherComment).HasMaxLength(2000);

        // Idempotent generation: re-running the generator for a template must not duplicate a
        // session it already created for the same UTC instant — see
        // docs/02 Модули/Scheduling.md → Инварианты. Partial (ScheduleTemplateId is nullable for
        // manually-created sessions, which are exempt from this constraint).
        builder.HasIndex(x => new { x.ScheduleTemplateId, x.StartUtc })
            .IsUnique()
            .HasFilter("\"ScheduleTemplateId\" IS NOT NULL");

        builder.HasIndex(x => x.StudyGroupId);
        builder.HasIndex(x => x.TeacherId);
        builder.HasIndex(x => x.RoomId);
        builder.HasIndex(x => x.StartUtc);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.LessonId);
        builder.HasIndex(x => x.RescheduledFromId);
    }
}
