using FluentValidation;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.UpdateStudyGroup;

public sealed class UpdateStudyGroupCommandValidator : AbstractValidator<UpdateStudyGroupCommand>
{
    public UpdateStudyGroupCommandValidator()
    {
        RuleFor(x => x.StudyGroupId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
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
