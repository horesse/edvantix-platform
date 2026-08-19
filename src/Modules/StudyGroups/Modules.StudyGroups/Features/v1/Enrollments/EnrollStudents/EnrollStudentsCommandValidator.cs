using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.EnrollStudents;

public sealed class EnrollStudentsCommandValidator : AbstractValidator<EnrollStudentsCommand>
{
    public EnrollStudentsCommandValidator()
    {
        RuleFor(x => x.StudyGroupId).NotEmpty();
        RuleFor(x => x.StudentIds).NotEmpty();
        RuleForEach(x => x.StudentIds).NotEmpty();
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
    }
}
