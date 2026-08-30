using FSH.Framework.Core.Domain;
using FSH.Modules.Curriculum.Contracts.Dtos;

namespace FSH.Modules.Curriculum.Domain;

/// <summary>
/// A material attached to a <see cref="Lesson"/>: a file, a video/link, a homework note, or a
/// presentation. Exactly one of <see cref="FileId"/>/<see cref="Url"/> is set — enforced here,
/// by <c>AddLessonMaterialCommandValidator</c>, and by a DB CHECK constraint
/// (<see cref="Data.Configurations.LessonMaterialConfiguration"/>). Which one is fixed by the
/// kind: <c>Video</c>/<c>Link</c> carry an external <see cref="Url"/> (a class recording lives on
/// an allow-listed video host — never a direct upload), <c>File</c>/<c>Presentation</c> carry a
/// stored <see cref="FileId"/>, <c>Homework</c> may be either.
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
        if (kind is MaterialKind.Video or MaterialKind.Link && fileId is not null)
        {
            throw new ArgumentException("Video and Link materials must use Url, not FileId.", nameof(kind));
        }
        if (kind is MaterialKind.File or MaterialKind.Presentation && !string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("File and Presentation materials must use FileId, not Url.", nameof(kind));
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
