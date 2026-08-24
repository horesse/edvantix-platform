using FluentValidation;
using FSH.Modules.Payments.Contracts.v1.Tariffs;

namespace FSH.Modules.Payments.Features.v1.Tariffs.UpdateTariff;

public sealed class UpdateTariffCommandValidator : AbstractValidator<UpdateTariffCommand>
{
    public UpdateTariffCommandValidator()
    {
        RuleFor(x => x.TariffId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LessonsCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ValidDays).GreaterThanOrEqualTo(0);
    }
}
