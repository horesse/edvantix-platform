using FluentValidation;
using FSH.Modules.Payments.Contracts.v1.Tariffs;

namespace FSH.Modules.Payments.Features.v1.Tariffs.DeactivateTariff;

public sealed class DeactivateTariffCommandValidator : AbstractValidator<DeactivateTariffCommand>
{
    public DeactivateTariffCommandValidator()
    {
        RuleFor(x => x.TariffId).NotEmpty();
    }
}
