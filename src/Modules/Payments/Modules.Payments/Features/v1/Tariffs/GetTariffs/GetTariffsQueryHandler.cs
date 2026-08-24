using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.Tariffs;
using FSH.Modules.Payments.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.Tariffs.GetTariffs;

public sealed class GetTariffsQueryHandler(PaymentsDbContext dbContext)
    : IQueryHandler<GetTariffsQuery, IReadOnlyList<TariffDto>>
{
    public async ValueTask<IReadOnlyList<TariffDto>> Handle(GetTariffsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tariffs = await dbContext.Tariffs
            .AsNoTracking()
            .Where(t => query.IsActive == null || t.IsActive == query.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return tariffs.Select(t => t.ToDto()).ToList();
    }
}
