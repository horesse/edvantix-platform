using FSH.Modules.Files.Contracts;

namespace FSH.Modules.Payments.Authorization;

/// <summary>
/// IFileAccessPolicy for payment-proof uploads (OwnerType=PaymentProof, OwnerId=InvoiceId — the
/// proof belongs to the invoice its <c>PaymentConfirmation</c> is recorded against, mirroring
/// Curriculum's <c>LessonMaterialAccessPolicy</c> which keys on the owning aggregate, not the
/// individual row).
///
/// - Attach: any authenticated user. The durable gate is <c>ConfirmPaymentCommand</c>'s own
///   permission check (<c>StudentPayments.Confirm</c> — see docs/02 Модули/Payments.md, the most
///   sensitive permission in the system).
/// - Read: open, same reasoning as <c>LessonMaterialAccessPolicy</c> — the durable gate is the
///   invoice/payments endpoints' own <c>StudentPayments.View</c> check, not this policy.
/// - Delete: uploader-only. In practice payment proofs are never deleted through the Files API —
///   a correction is a payment reversal (<c>ReversePaymentCommand</c>), not a file delete.
/// </summary>
public sealed class PaymentProofAccessPolicy : IFileAccessPolicy
{
    public string OwnerType => "PaymentProof";

    public Task<bool> CanAttachAsync(Guid? ownerId, string currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(!string.IsNullOrEmpty(currentUserId));

    public Task<bool> CanReadAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public Task<bool> CanDeleteAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(
            !string.IsNullOrEmpty(currentUserId)
            && string.Equals(currentUserId, context.CreatedByUserId, StringComparison.Ordinal));
    }
}
