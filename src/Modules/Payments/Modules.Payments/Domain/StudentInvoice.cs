using System.Net;
using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Payments.Contracts.Dtos;

namespace FSH.Modules.Payments.Domain;

/// <summary>
/// A student's bill for a period — the aggregate root for <see cref="Lines"/> (owned, mutable only
/// while <see cref="InvoiceStatus.Draft"/>) and <see cref="Payments"/> (owned, append-only —
/// corrections are reversal rows, never edits or removals). <see cref="Status"/> is never set
/// directly; it is always <see cref="Recalculate"/>d from <see cref="Total"/>/<see cref="PaidAmount"/>
/// (see docs/02 Модули/Payments.md → «Инварианты»).
/// </summary>
public sealed class StudentInvoice : AggregateRoot<Guid>
{
    public string Number { get; private set; } = default!;
    public Guid StudentId { get; private set; }
    public Guid? PayerGuardianId { get; private set; }
    public Guid? StudyGroupId { get; private set; }
    public DateOnly PeriodFrom { get; private set; }
    public DateOnly PeriodTo { get; private set; }
    public decimal Total { get; private set; }
    public decimal PaidAmount { get; private set; }
    public string Currency { get; private set; } = default!;
    public InvoiceStatus Status { get; private set; }
    public DateOnly? IssuedOn { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string? Comment { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private readonly List<InvoiceLine> _lines = [];
    public IReadOnlyList<InvoiceLine> Lines => _lines;

    private readonly List<PaymentConfirmation> _payments = [];
    public IReadOnlyList<PaymentConfirmation> Payments => _payments;

    /// <summary>True once <see cref="Status"/> ∈ {Issued, PartiallyPaid} and <see cref="DueDate"/> is
    /// in the past — never stored (see docs/02 Модули/Payments.md → «Инварианты»: "Overdue не
    /// хранится"). <paramref name="today"/> is passed in rather than read from the clock so query
    /// handlers/jobs can evaluate a whole batch against one consistent "now".</summary>
    public bool IsOverdue(DateOnly today) =>
        Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid && DueDate < today;

    private StudentInvoice() { }

    public static StudentInvoice Create(
        Guid studentId,
        Guid? payerGuardianId,
        Guid? studyGroupId,
        DateOnly periodFrom,
        DateOnly periodTo,
        DateOnly dueDate,
        string currency,
        string? comment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("StudentId is required.", nameof(studentId));
        }
        if (periodTo < periodFrom)
        {
            throw new ArgumentException("PeriodTo cannot precede PeriodFrom.", nameof(periodTo));
        }

        var id = Guid.CreateVersion7();
        return new StudentInvoice
        {
            Id = id,
            Number = GenerateNumber(id, DateOnly.FromDateTime(DateTime.UtcNow)),
            StudentId = studentId,
            PayerGuardianId = payerGuardianId,
            StudyGroupId = studyGroupId,
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            DueDate = dueDate,
            Currency = currency.Trim().ToUpperInvariant(),
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            Status = InvoiceStatus.Draft,
            Total = 0m,
            PaidAmount = 0m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Best-effort human-readable numbering — <c>INV-{year}-{short id}</c>. The final format
    /// (sequential per school, gaps on cancellation, etc.) is an open question — see
    /// docs/04 Задачи/Открытые вопросы.md → «Payments» → «Нумерация счетов». This is a working
    /// placeholder, not the resolved design.</summary>
    public static string GenerateNumber(Guid id, DateOnly issuedOrCreatedOn) =>
        $"INV-{issuedOrCreatedOn.Year}-{id.ToString("N", System.Globalization.CultureInfo.InvariantCulture)[..8].ToUpperInvariant()}";

    // ─── Draft editing ──────────────────────────────────────────────────────────────────

    public void UpdateHeader(Guid? payerGuardianId, Guid? studyGroupId, DateOnly periodFrom, DateOnly periodTo, DateOnly dueDate, string? comment)
    {
        EnsureDraft();
        if (periodTo < periodFrom)
        {
            throw new ArgumentException("PeriodTo cannot precede PeriodFrom.", nameof(periodTo));
        }

        PayerGuardianId = payerGuardianId;
        StudyGroupId = studyGroupId;
        PeriodFrom = periodFrom;
        PeriodTo = periodTo;
        DueDate = dueDate;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Replaces the whole line set — the simplest correct primitive for a `Draft`-only edit
    /// surface; the command handler builds the new set from the request and calls this once.</summary>
    public void ReplaceLines(IReadOnlyList<(string Description, Guid? TariffId, decimal Quantity, decimal UnitPrice)> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        EnsureDraft();

        _lines.Clear();
        foreach (var line in lines)
        {
            _lines.Add(InvoiceLine.Create(Id, line.Description, line.TariffId, line.Quantity, line.UnitPrice));
        }

        Total = decimal.Round(_lines.Sum(l => l.Amount), 2, MidpointRounding.AwayFromZero);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    // ─── Lifecycle: Draft → Issued → {PartiallyPaid → Paid} | Cancelled ────────────────────

    public void Issue(DateOnly issuedOn)
    {
        EnsureDraft();
        if (_lines.Count == 0)
        {
            throw new CustomException("Cannot issue an invoice with no lines.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        IssuedOn = issuedOn;
        Status = InvoiceStatus.Issued;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Only while nothing has been paid — once money has moved, a cancellation must be a
    /// reversal of every payment first (see docs/02 Модули/Payments.md → «Инварианты»).</summary>
    public void Cancel(string? reason)
    {
        if (Status == InvoiceStatus.Cancelled)
        {
            return;
        }
        if (Status == InvoiceStatus.Draft)
        {
            throw new CustomException("Cannot cancel a draft invoice — delete it instead.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }
        if (PaidAmount != 0m)
        {
            throw new CustomException(
                "Cannot cancel an invoice with payments recorded — reverse the payments first.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        Status = InvoiceStatus.Cancelled;
        Comment = string.IsNullOrWhiteSpace(reason) ? Comment : $"{Comment}\n[Cancelled] {reason.Trim()}".Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    // ─── Payments ───────────────────────────────────────────────────────────────────────

    /// <summary>Overpayment is allowed on purpose — the surplus becomes the student's advance
    /// balance (a read-side projection concern, computed when the balance is queried), it is not
    /// rejected here.</summary>
    public PaymentConfirmation ConfirmPayment(
        decimal amount, DateOnly paidOn, PaymentMethod method, string? reference, Guid? proofFileId, string confirmedByUserId, string? note)
    {
        if (Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
        {
            throw new CustomException(
                $"Cannot record a payment against an invoice in status {Status}.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        var payment = PaymentConfirmation.Create(Id, amount, paidOn, method, reference, proofFileId, confirmedByUserId, note);
        _payments.Add(payment);
        Recalculate();
        return payment;
    }

    public PaymentConfirmation ReversePayment(Guid paymentId, string reversedByUserId, string? note)
    {
        var original = _payments.FirstOrDefault(p => p.Id == paymentId)
            ?? throw new NotFoundException($"Payment {paymentId} not found on invoice {Id}.");
        if (original.ReversesId is not null)
        {
            throw new CustomException("Cannot reverse a reversal.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }
        if (_payments.Any(p => p.ReversesId == original.Id))
        {
            throw new CustomException("This payment has already been reversed.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        var reversal = PaymentConfirmation.CreateReversal(original, reversedByUserId, note);
        _payments.Add(reversal);
        Recalculate();
        return reversal;
    }

    /// <summary>The single source of truth for <see cref="PaidAmount"/>/<see cref="Status"/> — every
    /// mutation to <see cref="Payments"/> ends by calling this, nothing sets either property
    /// directly (see docs/02 Модули/Payments.md → «Статус выводится из сумм, вручную не задаётся»).</summary>
    public void Recalculate()
    {
        PaidAmount = decimal.Round(_payments.Sum(p => p.Amount), 2, MidpointRounding.AwayFromZero);

        if (Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
        {
            return;
        }

        Status = PaidAmount switch
        {
            <= 0m => InvoiceStatus.Issued,
            var paid when paid < Total => InvoiceStatus.PartiallyPaid,
            _ => InvoiceStatus.Paid,
        };
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void EnsureDraft()
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new CustomException(
                $"Invoice {Number} is {Status}; only a Draft invoice can be edited.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }
    }
}
