using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.ActivateStudyGroup;

public sealed class ActivateStudyGroupCommandValidator : AbstractValidator<ActivateStudyGroupCommand>
{
    public ActivateStudyGroupCommandValidator()
    {
        RuleFor(x => x.StudyGroupId).NotEmpty();
    }
}
