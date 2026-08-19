using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.Teachers;

namespace FSH.Modules.StudyGroups.Features.v1.Teachers.AddGroupTeacher;

public sealed class AddGroupTeacherCommandValidator : AbstractValidator<AddGroupTeacherCommand>
{
    public AddGroupTeacherCommandValidator()
    {
        RuleFor(x => x.StudyGroupId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}
