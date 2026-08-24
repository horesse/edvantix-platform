using FluentValidation;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.BulkIssueInvoices;

public sealed class BulkIssueInvoicesCommandValidator : AbstractValidator<BulkIssueInvoicesCommand>
{
    public BulkIssueInvoicesCommandValidator()
    {
        RuleFor(x => x.InvoiceIds).NotEmpty();
    }
}
