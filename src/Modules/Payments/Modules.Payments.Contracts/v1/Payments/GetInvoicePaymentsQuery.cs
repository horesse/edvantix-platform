using FSH.Modules.Payments.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.Payments;

public sealed record GetInvoicePaymentsQuery(Guid InvoiceId) : IQuery<IReadOnlyList<PaymentConfirmationDto>>;
