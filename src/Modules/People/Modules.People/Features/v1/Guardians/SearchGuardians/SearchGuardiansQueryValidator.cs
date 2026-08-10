using FluentValidation;
using FSH.Modules.People.Contracts.v1.Guardians;

namespace FSH.Modules.People.Features.v1.Guardians.SearchGuardians;

public sealed class SearchGuardiansQueryValidator : AbstractValidator<SearchGuardiansQuery>
{
    public SearchGuardiansQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
