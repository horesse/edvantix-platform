using FSH.Framework.Core.Domain;

namespace FSH.Modules.Curriculum.Domain;

/// <summary>A section of a course ("Раздел 1: Present Simple"). Owns <see cref="Lesson"/>s
/// through <see cref="Lesson.CourseModuleId"/> — queried/mutated directly by id, not through
/// a navigation collection (see docs/04 Задачи/Задачи · Новые модули.md → Curriculum →
/// "Плоская персистентность").</summary>
public sealed class CourseModule : AggregateRoot<Guid>
{
    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private CourseModule() { }

    public static CourseModule Create(Guid courseId, string title, string? description, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (courseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(courseId));
        }

        return new CourseModule
        {
            Id = Guid.CreateVersion7(),
            CourseId = courseId,
            Title = title.Trim(),
            Description = description?.Trim(),
            SortOrder = sortOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string title, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title.Trim();
        Description = description?.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    internal void SetSortOrder(int order)
    {
        SortOrder = order;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
