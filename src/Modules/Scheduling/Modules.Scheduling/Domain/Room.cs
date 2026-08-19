using FSH.Framework.Core.Domain;

namespace FSH.Modules.Scheduling.Domain;

/// <summary>
/// A physical or virtual classroom. <see cref="IsVirtual"/> rooms are exempt from the "one session
/// at a time" resource conflict check — see <c>ISessionConflictChecker</c> and
/// docs/02 Модули/Scheduling.md → "Конфликты". No soft delete: rooms are simple reference data, like
/// Curriculum's <c>Subject</c>/<c>CourseModule</c> — no restore command in the contracts.
/// </summary>
public sealed class Room : BaseEntity<Guid>
{
    public string Name { get; private set; } = default!;
    public int Capacity { get; private set; }
    public string? Location { get; private set; }
    public bool IsVirtual { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private Room() { }

    public static Room Create(string name, int capacity, string? location, bool isVirtual)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");
        }

        return new Room
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            Capacity = capacity,
            Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim(),
            IsVirtual = isVirtual,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string name, int capacity, string? location, bool isVirtual)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");
        }

        Name = name.Trim();
        Capacity = capacity;
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        IsVirtual = isVirtual;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
