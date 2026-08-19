using FSH.Framework.Core.Domain;

namespace FSH.Modules.Scheduling.Domain;

/// <summary>
/// A school holiday/non-working day. The session generator skips <see cref="Date"/> entirely rather
/// than shifting to the next working day — shifting breaks regularity and creates conflicts with
/// other groups (see docs/04 Задачи/Открытые вопросы.md → Scheduling → "Праздники").
/// </summary>
public sealed class NonWorkingDay : BaseEntity<Guid>
{
    public DateOnly Date { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private NonWorkingDay() { }

    public static NonWorkingDay Create(DateOnly date, string? description) => new()
    {
        Id = Guid.CreateVersion7(),
        Date = date,
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };
}
