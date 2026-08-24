using FSH.Framework.Core.Domain;
using FSH.Modules.Payments.Contracts.Dtos;

namespace FSH.Modules.Payments.Domain;

/// <summary>
/// A pricing plan a student is charged against — either directly (<c>OneTime</c>) or through a
/// <see cref="Contracts.Dtos.TariffKind"/>-specific accrual (<c>PerLesson</c>/<c>PerMonth</c>/
/// <c>PerPackage</c>, see <c>ITariffAccrualService</c>). Simple reference data, like Scheduling's
/// <c>Room</c> — no soft delete, deactivation only (<see cref="Deactivate"/>).
/// </summary>
public sealed class Tariff : BaseEntity<Guid>
{
    public string Name { get; private set; } = default!;
    public Guid? CourseId { get; private set; }
    public TariffKind Kind { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;

    /// <summary>Package size for <see cref="TariffKind.PerPackage"/> — number of lessons prepaid.</summary>
    public int LessonsCount { get; private set; }

    /// <summary>Validity window in days for <see cref="TariffKind.PerPackage"/> — after this many days
    /// from issue the unused balance of the package is no longer chargeable against new sessions.</summary>
    public int ValidDays { get; private set; }

    public bool ChargeOnExcusedAbsence { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private Tariff() { }

    public static Tariff Create(
        string name,
        Guid? courseId,
        TariffKind kind,
        decimal amount,
        string currency,
        int lessonsCount,
        int validDays,
        bool chargeOnExcusedAbsence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        }

        return new Tariff
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            CourseId = courseId,
            Kind = kind,
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(),
            LessonsCount = lessonsCount,
            ValidDays = validDays,
            ChargeOnExcusedAbsence = chargeOnExcusedAbsence,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(
        string name,
        Guid? courseId,
        decimal amount,
        int lessonsCount,
        int validDays,
        bool chargeOnExcusedAbsence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        }

        // Kind/Currency are not editable — a tariff already referenced by issued invoices must keep
        // the accrual rule and currency it was issued under; create a new tariff instead.
        Name = name.Trim();
        CourseId = courseId;
        Amount = amount;
        LessonsCount = lessonsCount;
        ValidDays = validDays;
        ChargeOnExcusedAbsence = chargeOnExcusedAbsence;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
