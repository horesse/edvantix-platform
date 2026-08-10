using FSH.Framework.Core.Domain;

namespace FSH.Modules.Curriculum.Domain;

/// <summary>
/// A program lesson ("Урок 7: Past Simple") — a topic/objectives/materials template, with no
/// date, teacher, or students. Not to be confused with <c>Scheduling.Session</c>, the calendar
/// event (see docs/05 Решения (ADR)/ADR-006 Урок программы и занятие расписания).
/// </summary>
public sealed class Lesson : AggregateRoot<Guid>
{
    public Guid CourseModuleId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Objectives { get; private set; }
    public string? Content { get; private set; }
    public int DurationMinutes { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private Lesson() { }

    public static Lesson Create(
        Guid courseModuleId,
        string title,
        string? objectives,
        string? content,
        int durationMinutes,
        int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (courseModuleId == Guid.Empty)
        {
            throw new ArgumentException("CourseModuleId is required.", nameof(courseModuleId));
        }
        if (durationMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Duration cannot be negative.");
        }

        return new Lesson
        {
            Id = Guid.CreateVersion7(),
            CourseModuleId = courseModuleId,
            Title = title.Trim(),
            Objectives = objectives?.Trim(),
            Content = content?.Trim(),
            DurationMinutes = durationMinutes,
            SortOrder = sortOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string title, string? objectives, string? content, int durationMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (durationMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Duration cannot be negative.");
        }

        Title = title.Trim();
        Objectives = objectives?.Trim();
        Content = content?.Trim();
        DurationMinutes = durationMinutes;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    internal void SetSortOrder(int order)
    {
        SortOrder = order;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
