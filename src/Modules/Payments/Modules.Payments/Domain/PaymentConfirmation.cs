using FSH.Framework.Core.Domain;
using FSH.Modules.Payments.Contracts.Dtos;

namespace FSH.Modules.Payments.Domain;

/// <summary>
/// A manager's assertion that money changed hands — there is no payment gateway, so this record is
/// the whole trust boundary (see docs/02 Модули/Payments.md → «Платёжного шлюза нет»). Immutable
/// once created: a correction is a new row with <see cref="ReversesId"/> set
/// (<see cref="StudentInvoice.ReversePayment"/>), never an edit of this one.
/// </summary>
public sealed class PaymentConfirmation : BaseEntity<Guid>
{
    public Guid InvoiceId { get; private set; }

    /// <summary>Signed — a reversal row carries the negated amount of the payment it reverses, so
    /// <c>StudentInvoice.PaidAmount</c> stays a plain sum over all rows (see <see cref="StudentInvoice.Recalculate"/>).</summary>
    public decimal Amount { get; private set; }

    public DateOnly PaidOn { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? Reference { get; private set; }
    public Guid? ProofFileId { get; private set; }
    public string ConfirmedByUserId { get; private set; } = default!;
    public DateTimeOffset ConfirmedAtUtc { get; private set; }

    /// <summary>Set only on a reversal row — the id of the <see cref="PaymentConfirmation"/> it
    /// reverses. Null on every ordinary payment.</summary>
    public Guid? ReversesId { get; private set; }

    public string? Note { get; private set; }

    private PaymentConfirmation() { }

    internal static PaymentConfirmation Create(
        Guid invoiceId,
        decimal amount,
        DateOnly paidOn,
        PaymentMethod method,
        string? reference,
        Guid? proofFileId,
        string confirmedByUserId,
        string? note)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be positive.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmedByUserId);

        return new PaymentConfirmation
        {
            Id = Guid.CreateVersion7(),
            InvoiceId = invoiceId,
            Amount = amount,
            PaidOn = paidOn,
            Method = method,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            ProofFileId = proofFileId,
            ConfirmedByUserId = confirmedByUserId,
            ConfirmedAtUtc = DateTimeOffset.UtcNow,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };
    }

    internal static PaymentConfirmation CreateReversal(PaymentConfirmation original, string confirmedByUserId, string? note)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmedByUserId);

        return new PaymentConfirmation
        {
            Id = Guid.CreateVersion7(),
            InvoiceId = original.InvoiceId,
            Amount = -original.Amount,
            PaidOn = original.PaidOn,
            Method = original.Method,
            Reference = original.Reference,
            ProofFileId = null,
            ConfirmedByUserId = confirmedByUserId,
            ConfirmedAtUtc = DateTimeOffset.UtcNow,
            ReversesId = original.Id,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };
    }
}
