using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.Tariffs;

public sealed record DeactivateTariffCommand(Guid TariffId) : ICommand<Unit>;
