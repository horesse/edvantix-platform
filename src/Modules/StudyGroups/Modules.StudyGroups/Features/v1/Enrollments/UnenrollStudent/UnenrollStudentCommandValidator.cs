using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.UnenrollStudent;

public sealed class UnenrollStudentCommandValidator : AbstractValidator<UnenrollStudentCommand>
{
    public UnenrollStudentCommandValidator()
    {
        RuleFor(x => x.StudyGroupId).NotEmpty();
        RuleFor(x => x.EnrollmentId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}
