using FSH.Framework.Core.Domain;

namespace FSH.Modules.Curriculum.Domain;

/// <summary>
/// A direction/subject in the curriculum tree ("Английский язык" → "Английский для детей").
/// Purely a classification node — no soft delete/restore in the contracts
/// (<c>DeleteSubjectCommand</c> is a hard delete; see docs/04 Задачи/Задачи · Новые модули.md
/// → Curriculum → "Проектные решения").
/// </summary>
public sealed class Subject : AggregateRoot<Guid>
{
    public Guid? ParentId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private Subject() { }

    public static Subject Create(string name, Guid? parentId, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Subject
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            Slug = Slugify(name),
            ParentId = parentId,
            SortOrder = sortOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string name, Guid? parentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (parentId == Id)
        {
            throw new InvalidOperationException("A subject cannot be its own parent.");
        }

        Name = name.Trim();
        Slug = Slugify(name);
        ParentId = parentId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    internal void SetSortOrder(int order)
    {
        SortOrder = order;
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
