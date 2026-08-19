using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.CancelStudyGroup;

public sealed class CancelStudyGroupCommandValidator : AbstractValidator<CancelStudyGroupCommand>
{
    public CancelStudyGroupCommandValidator()
    {
        RuleFor(x => x.StudyGroupId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}
