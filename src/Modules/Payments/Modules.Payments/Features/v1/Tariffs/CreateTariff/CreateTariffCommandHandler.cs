using FSH.Modules.Payments.Contracts.v1.Tariffs;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Domain;
using Mediator;

namespace FSH.Modules.Payments.Features.v1.Tariffs.CreateTariff;

public sealed class CreateTariffCommandHandler(PaymentsDbContext dbContext)
    : ICommandHandler<CreateTariffCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateTariffCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tariff = Tariff.Create(
            command.Name,
            command.CourseId,
            command.Kind,
            command.Amount,
            command.Currency,
            command.LessonsCount,
            command.ValidDays,
            command.ChargeOnExcusedAbsence);

        dbContext.Tariffs.Add(tariff);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return tariff.Id;
    }
}
