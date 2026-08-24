using FluentValidation;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.IssueInvoice;

public sealed class IssueInvoiceCommandValidator : AbstractValidator<IssueInvoiceCommand>
{
    public IssueInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
    }
}
