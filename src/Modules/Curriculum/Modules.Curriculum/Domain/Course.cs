using System.Net;
using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.Dtos;

namespace FSH.Modules.Curriculum.Domain;

/// <summary>
/// A course template — a shelf item in the program, outside of time and people. Dates come
/// from Scheduling, students from StudyGroups (see docs/02 Модули/Curriculum.md).
/// <para>
/// Only <see cref="Course"/> is <see cref="ISoftDeletable"/> among the Curriculum entities —
/// <see cref="CourseModule"/>/<see cref="Lesson"/>/<see cref="LessonMaterial"/> have no restore
/// command in the contracts, so their delete/remove handlers hard-delete instead.
/// </para>
/// </summary>
public sealed class Course : AggregateRoot<Guid>, ISoftDeletable
{
    public Guid SubjectId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public CourseLevel Level { get; private set; }
    public int DurationHours { get; private set; }
    public CourseStatus Status { get; private set; }
    public Guid? CoverFileId { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Course() { }

    public static Course Create(
        Guid subjectId,
        string title,
        string? description,
        CourseLevel level,
        int durationHours,
        Guid? coverFileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (subjectId == Guid.Empty)
        {
            throw new ArgumentException("SubjectId is required.", nameof(subjectId));
        }
        if (durationHours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationHours), "Duration cannot be negative.");
        }

        return new Course
        {
            Id = Guid.CreateVersion7(),
            SubjectId = subjectId,
            Title = title.Trim(),
            Slug = Slugify(title),
            Description = description?.Trim(),
            Level = level,
            DurationHours = durationHours,
            CoverFileId = coverFileId,
            Status = CourseStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(
        Guid subjectId,
        string title,
        string? description,
        CourseLevel level,
        int durationHours,
        Guid? coverFileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (subjectId == Guid.Empty)
        {
            throw new ArgumentException("SubjectId is required.", nameof(subjectId));
        }
        if (durationHours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationHours), "Duration cannot be negative.");
        }

        SubjectId = subjectId;
        Title = title.Trim();
        Slug = Slugify(title);
        Description = description?.Trim();
        Level = level;
        DurationHours = durationHours;
        CoverFileId = coverFileId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Draft/Published → Published. Caller (handler) has already checked the course
    /// has at least one <see cref="CourseModule"/> — "курс без разделов недопустим" (see
    /// docs/02 Модули/Curriculum.md → Инварианты).</summary>
    public void Publish()
    {
        if (Status == CourseStatus.Published)
        {
            return;
        }
        if (Status == CourseStatus.Archived)
        {
            throw new CustomException(
                "Cannot publish an archived course.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        Status = CourseStatus.Published;
        PublishedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Draft/Published → Archived. Archiving does not break existing study groups —
    /// it only blocks new ones (enforced by StudyGroups via <c>ICourseQueryService.IsPublishedAsync</c>).</summary>
    public void Archive()
    {
        if (Status == CourseStatus.Archived)
        {
            return;
        }

        Status = CourseStatus.Archived;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string Slugify(string value)
    {
        var trimmed = value.Trim();
#pragma warning disable CA1308 // slug is canonical lowercase, not security-sensitive
        var lower = trimmed.ToLowerInvariant();
#pragma warning restore CA1308
        var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var collapsed = new string(chars).Trim('-');
        while (collapsed.Contains("--", StringComparison.Ordinal))
        {
            collapsed = collapsed.Replace("--", "-", StringComparison.Ordinal);
        }
        return collapsed;
    }
}
