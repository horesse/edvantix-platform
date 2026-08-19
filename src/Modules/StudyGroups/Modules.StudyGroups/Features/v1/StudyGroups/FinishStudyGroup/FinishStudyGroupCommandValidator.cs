using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.FinishStudyGroup;

public sealed class FinishStudyGroupCommandValidator : AbstractValidator<FinishStudyGroupCommand>
{
    public FinishStudyGroupCommandValidator()
    {
        RuleFor(x => x.StudyGroupId).NotEmpty();
    }
}
