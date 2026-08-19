using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Calendar;

namespace FSH.Modules.Scheduling.Features.v1.Calendar.AddNonWorkingDay;

public sealed class AddNonWorkingDayCommandValidator : AbstractValidator<AddNonWorkingDayCommand>
{
    public AddNonWorkingDayCommandValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(256);
    }
}
