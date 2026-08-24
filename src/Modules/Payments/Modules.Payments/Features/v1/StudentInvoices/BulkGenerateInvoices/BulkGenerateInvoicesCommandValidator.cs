using FluentValidation;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.BulkGenerateInvoices;

public sealed class BulkGenerateInvoicesCommandValidator : AbstractValidator<BulkGenerateInvoicesCommand>
{
    public BulkGenerateInvoicesCommandValidator()
    {
        RuleFor(x => x.StudyGroupId).NotEmpty();
        RuleFor(x => x.PeriodTo).GreaterThanOrEqualTo(x => x.PeriodFrom);
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.PeriodFrom);
    }
}
