using FSH.Framework.Core.Domain;
using FSH.Modules.Curriculum.Contracts.Dtos;

namespace FSH.Modules.Curriculum.Domain;

/// <summary>
/// A material attached to a <see cref="Lesson"/>: a file, a video/link, a homework note, or a
/// presentation. Exactly one of <see cref="FileId"/>/<see cref="Url"/> is set — enforced here,
/// by <c>AddLessonMaterialCommandValidator</c>, and by a DB CHECK constraint
/// (<see cref="Data.Configurations.LessonMaterialConfiguration"/>).
/// <see cref="VisibleToStudents"/>&#160;=&#160;false marks teacher-only material (answer keys,
/// methodology notes).
/// </summary>
public sealed class LessonMaterial : AggregateRoot<Guid>
{
    public Guid LessonId { get; private set; }
    public MaterialKind Kind { get; private set; }
    public string Title { get; private set; } = default!;
    public Guid? FileId { get; private set; }
    public string? Url { get; private set; }
    public bool VisibleToStudents { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private LessonMaterial() { }

    public static LessonMaterial Create(
        Guid lessonId,
        MaterialKind kind,
        string title,
        Guid? fileId,
        string? url,
        bool visibleToStudents,
        int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (lessonId == Guid.Empty)
        {
            throw new ArgumentException("LessonId is required.", nameof(lessonId));
        }
        if (fileId is null == string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Exactly one of FileId or Url must be set.");
        }

        return new LessonMaterial
        {
            Id = Guid.CreateVersion7(),
            LessonId = lessonId,
            Kind = kind,
            Title = title.Trim(),
            FileId = fileId,
            Url = string.IsNullOrWhiteSpace(url) ? null : url.Trim(),
            VisibleToStudents = visibleToStudents,
            SortOrder = sortOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    internal void SetSortOrder(int order) => SortOrder = order;
}
