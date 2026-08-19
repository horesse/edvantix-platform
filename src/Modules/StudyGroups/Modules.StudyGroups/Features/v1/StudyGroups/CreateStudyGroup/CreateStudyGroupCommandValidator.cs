using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.CreateStudyGroup;

public sealed class CreateStudyGroupCommandValidator : AbstractValidator<CreateStudyGroupCommand>
{
    public CreateStudyGroupCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.PrimaryTeacherId).NotEmpty();
        RuleFor(x => x.Format).IsInEnum();
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(1);
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate is not null)
            .WithMessage("EndDate must not be before StartDate.");
        RuleFor(x => x.MeetingUrl).MaximumLength(512);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
