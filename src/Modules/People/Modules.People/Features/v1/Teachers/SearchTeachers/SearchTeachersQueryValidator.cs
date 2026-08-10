using FluentValidation;
using FSH.Modules.People.Contracts.v1.Teachers;

namespace FSH.Modules.People.Features.v1.Teachers.SearchTeachers;

public sealed class SearchTeachersQueryValidator : AbstractValidator<SearchTeachersQuery>
{
    public SearchTeachersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
