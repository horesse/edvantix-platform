using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.TransferEnrollment;

public sealed class TransferEnrollmentCommandValidator : AbstractValidator<TransferEnrollmentCommand>
{
    public TransferEnrollmentCommandValidator()
    {
        RuleFor(x => x.EnrollmentId).NotEmpty();
        RuleFor(x => x.TargetStudyGroupId).NotEmpty();
    }
}
