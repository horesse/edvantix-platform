using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Calendar;

namespace FSH.Modules.Scheduling.Features.v1.Calendar.RemoveNonWorkingDay;

public sealed class RemoveNonWorkingDayCommandValidator : AbstractValidator<RemoveNonWorkingDayCommand>
{
    public RemoveNonWorkingDayCommandValidator()
    {
        RuleFor(x => x.NonWorkingDayId).NotEmpty();
    }
}
