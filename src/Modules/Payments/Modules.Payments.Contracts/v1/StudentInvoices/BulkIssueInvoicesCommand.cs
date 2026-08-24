using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.StudentInvoices;

/// <summary>Best-effort — invoices that are not <c>Draft</c> (or do not exist) are silently skipped
/// rather than failing the whole batch; the return value is the ids actually issued.</summary>
public sealed record BulkIssueInvoicesCommand(IReadOnlyList<Guid> InvoiceIds, DateOnly IssuedOn) : ICommand<IReadOnlyList<Guid>>;
