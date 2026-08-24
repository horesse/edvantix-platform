using FluentValidation;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.SearchStudentInvoices;

public sealed class SearchStudentInvoicesQueryValidator : AbstractValidator<SearchStudentInvoicesQuery>
{
    public SearchStudentInvoicesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
