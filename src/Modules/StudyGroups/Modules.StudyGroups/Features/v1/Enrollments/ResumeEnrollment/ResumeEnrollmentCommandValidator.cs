using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.ResumeEnrollment;

public sealed class ResumeEnrollmentCommandValidator : AbstractValidator<ResumeEnrollmentCommand>
{
    public ResumeEnrollmentCommandValidator()
    {
        RuleFor(x => x.EnrollmentId).NotEmpty();
    }
}
