using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1.Guardians;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Guardians.GetGuardianById;

public sealed class GetGuardianByIdQueryHandler(PeopleDbContext dbContext)
    : IQueryHandler<GetGuardianByIdQuery, GuardianDto>
{
    public async ValueTask<GuardianDto> Handle(GetGuardianByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var g = await dbContext.Guardians
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.GuardianId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Guardian {query.GuardianId} not found.");

        return ToDto(g);
    }

    internal static GuardianDto ToDto(Guardian g) =>
        new(g.Id, g.LastName, g.FirstName, g.DisplayName, g.Phone, g.Email, g.UserId);
}
