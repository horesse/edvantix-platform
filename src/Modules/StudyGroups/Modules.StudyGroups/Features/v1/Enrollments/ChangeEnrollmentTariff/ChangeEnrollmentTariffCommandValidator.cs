using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.ChangeEnrollmentTariff;

public sealed class ChangeEnrollmentTariffCommandValidator : AbstractValidator<ChangeEnrollmentTariffCommand>
{
    public ChangeEnrollmentTariffCommandValidator()
    {
        RuleFor(x => x.EnrollmentId).NotEmpty();
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
    }
}
