using FSH.Modules.Files.Contracts;
using FSH.Modules.Tickets.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Tickets.Authorization;

/// <summary>
/// <see cref="IFileAccessPolicy"/> for ticket attachments (<c>OwnerType=Ticket</c>,
/// <c>OwnerId=TicketId</c>). Clients attach files through the generic Files endpoints
/// (<c>RequestUploadUrl</c> with <c>ownerType=Ticket</c>) — there is no ticket-scoped file endpoint,
/// so this policy is the only authorization gate.
///
/// - Attach / Read: caller must be the ticket's <c>ReporterUserId</c> or its
///   <c>AssignedToUserId</c>. This mirrors Chat's membership-only rule; a support agent who needs
///   access gets it by being assigned the ticket (<c>AssignTicket</c>).
/// - Delete / visibility: uploader only.
///
/// Tenant scoping is enforced upstream by <c>BaseDbContext</c>.
/// </summary>
public sealed class TicketFileAccessPolicy(TicketsDbContext db) : IFileAccessPolicy
{
    /// <summary>Owner-type token clients pass on RequestUploadUrl (and that we read here).</summary>
    public const string OwnerTypeName = "Ticket";

    public string OwnerType => OwnerTypeName;

    public Task<bool> CanAttachAsync(Guid? ownerId, string currentUserId, CancellationToken cancellationToken) =>
        IsParticipantAsync(ownerId, currentUserId, cancellationToken);

    public Task<bool> CanReadAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return IsParticipantAsync(context.OwnerId, currentUserId, cancellationToken);
    }

    public Task<bool> CanDeleteAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(
            !string.IsNullOrEmpty(currentUserId)
            && string.Equals(currentUserId, context.CreatedByUserId, StringComparison.Ordinal));
    }

    private async Task<bool> IsParticipantAsync(Guid? ownerId, string currentUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(currentUserId) || ownerId is not { } ticketId)
        {
            return false;
        }

        if (!Guid.TryParse(currentUserId, out var userId))
        {
            return false;
        }

        return await db.Tickets.AsNoTracking()
            .AnyAsync(
                t => t.Id == ticketId && (t.ReporterUserId == userId || t.AssignedToUserId == userId),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
