using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.DeleteStudyGroup;

public sealed class DeleteStudyGroupCommandValidator : AbstractValidator<DeleteStudyGroupCommand>
{
    public DeleteStudyGroupCommandValidator()
    {
        RuleFor(x => x.StudyGroupId).NotEmpty();
    }
}
