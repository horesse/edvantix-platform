using FluentValidation;
using FSH.Modules.Payments.Contracts.v1.Tariffs;

namespace FSH.Modules.Payments.Features.v1.Tariffs.CreateTariff;

public sealed class CreateTariffCommandValidator : AbstractValidator<CreateTariffCommand>
{
    public CreateTariffCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LessonsCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ValidDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Kind).IsInEnum();
    }
}
