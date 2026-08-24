using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.StudentInvoices;

public sealed record CancelInvoiceCommand(Guid InvoiceId, string? Reason) : ICommand<Unit>;
