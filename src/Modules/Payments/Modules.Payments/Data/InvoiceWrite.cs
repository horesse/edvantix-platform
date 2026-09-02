using System.Diagnostics;
using System.Net;
using FSH.Framework.Core.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Data;

/// <summary>
/// Runs an invoice-mutating unit of work with a bounded reload-and-retry on an optimistic-concurrency
/// failure.
/// <para>
/// <see cref="Domain.StudentInvoice"/> carries no row-version column, so when another write touches
/// the same invoice between the handler's read and its <c>SaveChanges</c> — a second payment on the
/// same invoice, a draft refresh, an accrual job, a double-submit from the UI — EF Core surfaces it
/// as a bare <see cref="DbUpdateConcurrencyException"/> ("expected to affect 1 row(s), but actually
/// affected 0 row(s)") which otherwise escapes as an opaque HTTP 500. This helper detaches the stale
/// graph, re-runs the unit of work against a fresh read, and only gives up — as a clean
/// <see cref="HttpStatusCode.Conflict"/> — once the attempt budget is spent.
/// </para>
/// </summary>
internal static class InvoiceWrite
{
    public static async ValueTask<T> WithConcurrencyRetryAsync<T>(
        PaymentsDbContext dbContext,
        Func<CancellationToken, ValueTask<T>> unitOfWork,
        CancellationToken cancellationToken,
        int maxAttempts = 3)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await unitOfWork(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                DetachAll(dbContext);
                if (attempt == maxAttempts)
                {
                    throw new CustomException(
                        "The invoice was modified by another operation. Reload it and try again.",
                        ex,
                        HttpStatusCode.Conflict);
                }
            }
        }

        throw new UnreachableException();
    }

    /// <summary>Drop every tracked entry so the next attempt reads a clean copy — a failed
    /// <c>SaveChanges</c> leaves the added/modified entries in place, and re-saving them would replay
    /// the same doomed batch.</summary>
    private static void DetachAll(PaymentsDbContext dbContext)
    {
        foreach (var entry in dbContext.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }
}
