using FluentValidation;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.CancelInvoice;

public sealed class CancelInvoiceCommandValidator : AbstractValidator<CancelInvoiceCommand>
{
    public CancelInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
