namespace FSH.Modules.Payments.Services;

/// <summary>
/// Hands out <c>StudentInvoice.Number</c> values for the current tenant from its configurable template
/// (<c>TenantSettings.InvoiceNumberTemplate</c>, EDX-013), backed by a concurrency-safe per-tenant
/// counter. See docs/02 Модули/Payments.md → «Нумерация счетов».
/// </summary>
public interface IInvoiceNumberGenerator
{
    /// <summary>Reserves and formats a single invoice number.</summary>
    ValueTask<string> NextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserves a contiguous block of <paramref name="count"/> numbers in one atomic step and returns
    /// them formatted, in ascending order — the batch path (<c>BulkGenerateInvoicesCommand</c>) so a
    /// class-sized run never round-trips the counter per student or races another batch.
    /// </summary>
    ValueTask<IReadOnlyList<string>> NextBatchAsync(int count, CancellationToken cancellationToken = default);
}
