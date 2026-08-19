using FSH.Modules.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Scheduling.Data.Configurations;

public sealed class ScheduleTemplateConfiguration : IEntityTypeConfiguration<ScheduleTemplate>
{
    public void Configure(EntityTypeBuilder<ScheduleTemplate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ScheduleTemplates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek).IsRequired().HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.StartTime).IsRequired();

        // No DB-level FK on StudyGroupId/RoomId/TeacherId. StudyGroupId/TeacherId are cross-module
        // references (architecture.md rule 1 — no FK across module boundaries by design). RoomId
        // references Room in this SAME module/schema but is kept a plain Guid? for uniformity with
        // the other two and because "room no longer exists" should degrade to "room unset", not
        // break the template — handlers validate existence explicitly via a query, not a constraint.
        builder.HasIndex(x => x.StudyGroupId);
        builder.HasIndex(x => x.RoomId);
        builder.HasIndex(x => x.TeacherId);
        builder.HasIndex(x => x.IsActive);
    }
}
