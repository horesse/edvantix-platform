using FSH.Framework.Core.Domain;

namespace FSH.Modules.People.Domain;

/// <summary>
/// An internal note about a student, visible only to holders of <c>Students.ViewNotes</c>
/// (teachers do not see these — see docs/02 Модули/People.md). Owned by <see cref="Student"/>.
/// Immutable once created: there is no update, only add/soft-delete.
/// </summary>
public sealed class StudentNote : BaseEntity<Guid>, ISoftDeletable
{
    public Guid StudentId { get; private set; }
    public string Text { get; private set; } = default!;
    public string AuthorUserId { get; private set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private StudentNote() { }

    internal static StudentNote Create(Guid studentId, string text, string authorUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(authorUserId);

        return new StudentNote
        {
            Id = Guid.CreateVersion7(),
            StudentId = studentId,
            Text = text.Trim(),
            AuthorUserId = authorUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
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
    }
}
