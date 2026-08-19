using FSH.Framework.Core.Domain;

namespace FSH.Modules.Scheduling.Domain;

/// <summary>
/// A recurrence template — "every Tuesday at 18:00" for a study group. The generator expands this
/// into <see cref="Session"/> rows for a rolling horizon. <see cref="StartTime"/> is stored as
/// **local** school time (see docs/02 Модули/Scheduling.md → "Время") — the one place in the system
/// where local time is persisted; every <see cref="Session"/> generated from it stores UTC. No soft
/// delete: templates are configuration, not history (already-generated <see cref="Session"/> rows
/// survive a template's deletion via <see cref="Session.ScheduleTemplateId"/> staying nullable/orphaned).
/// </summary>
public sealed class ScheduleTemplate : BaseEntity<Guid>
{
    public Guid StudyGroupId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public int DurationMinutes { get; private set; }
    public Guid? RoomId { get; private set; }
    public Guid? TeacherId { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private ScheduleTemplate() { }

    public static ScheduleTemplate Create(
        Guid studyGroupId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        int durationMinutes,
        Guid? roomId,
        Guid? teacherId,
        DateOnly validFrom,
        DateOnly? validTo)
    {
        if (studyGroupId == Guid.Empty)
        {
            throw new ArgumentException("StudyGroupId is required.", nameof(studyGroupId));
        }

        ValidateDuration(durationMinutes);
        ValidateRange(validFrom, validTo);

        return new ScheduleTemplate
        {
            Id = Guid.CreateVersion7(),
            StudyGroupId = studyGroupId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            DurationMinutes = durationMinutes,
            RoomId = roomId,
            TeacherId = teacherId,
            ValidFrom = validFrom,
            ValidTo = validTo,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        int durationMinutes,
        Guid? roomId,
        Guid? teacherId,
        DateOnly validFrom,
        DateOnly? validTo,
        bool isActive)
    {
        ValidateDuration(durationMinutes);
        ValidateRange(validFrom, validTo);

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        DurationMinutes = durationMinutes;
        RoomId = roomId;
        TeacherId = teacherId;
        ValidFrom = validFrom;
        ValidTo = validTo;
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>True on <paramref name="date"/> when the template is active and the date falls
    /// inside [<see cref="ValidFrom"/>, <see cref="ValidTo"/>] (inclusive, open-ended if
    /// <see cref="ValidTo"/> is null) and lands on <see cref="DayOfWeek"/>.</summary>
    public bool AppliesOn(DateOnly date) =>
        IsActive
        && date.DayOfWeek == DayOfWeek
        && date >= ValidFrom
        && (ValidTo is null || date <= ValidTo.Value);

    private static void ValidateDuration(int durationMinutes)
    {
        if (durationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "DurationMinutes must be positive.");
        }
    }

    private static void ValidateRange(DateOnly validFrom, DateOnly? validTo)
    {
        if (validTo is not null && validTo.Value < validFrom)
        {
            throw new ArgumentException("ValidTo cannot be before ValidFrom.", nameof(validTo));
        }
    }
}
