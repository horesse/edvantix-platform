using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.StudentInvoices;

public sealed record GetInvoicePdfQuery(Guid InvoiceId) : IQuery<byte[]>;
