using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.StudentInvoices;

public sealed record IssueInvoiceCommand(Guid InvoiceId, DateOnly IssuedOn) : ICommand<Unit>;
