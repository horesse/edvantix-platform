using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.Teachers;

namespace FSH.Modules.StudyGroups.Features.v1.Teachers.RemoveGroupTeacher;

public sealed class RemoveGroupTeacherCommandValidator : AbstractValidator<RemoveGroupTeacherCommand>
{
    public RemoveGroupTeacherCommandValidator()
    {
        RuleFor(x => x.StudyGroupId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty();
    }
}
