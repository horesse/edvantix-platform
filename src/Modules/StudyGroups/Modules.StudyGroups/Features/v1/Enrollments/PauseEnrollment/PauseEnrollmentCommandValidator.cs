using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.PauseEnrollment;

public sealed class PauseEnrollmentCommandValidator : AbstractValidator<PauseEnrollmentCommand>
{
    public PauseEnrollmentCommandValidator()
    {
        RuleFor(x => x.EnrollmentId).NotEmpty();
    }
}
