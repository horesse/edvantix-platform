using FSH.Framework.Core.Exceptions;
using FSH.Modules.Payments.Contracts.v1.Tariffs;
using FSH.Modules.Payments.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.Tariffs.DeactivateTariff;

public sealed class DeactivateTariffCommandHandler(PaymentsDbContext dbContext)
    : ICommandHandler<DeactivateTariffCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeactivateTariffCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tariff = await dbContext.Tariffs
            .FirstOrDefaultAsync(t => t.Id == command.TariffId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Tariff {command.TariffId} not found.");

        tariff.Deactivate();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
