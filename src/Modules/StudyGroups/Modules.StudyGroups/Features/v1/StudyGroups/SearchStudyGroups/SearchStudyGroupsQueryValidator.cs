using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.SearchStudyGroups;

public sealed class SearchStudyGroupsQueryValidator : AbstractValidator<SearchStudyGroupsQuery>
{
    public SearchStudyGroupsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
