using FSH.Framework.Core.Context;
using FSH.Modules.People.Contracts;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1;
using Mediator;

namespace FSH.Modules.People.Features.v1.GetMyPeopleScope;

public sealed class GetMyPeopleScopeQueryHandler(IPeopleScopeResolver scopeResolver, ICurrentUser currentUser)
    : IQueryHandler<GetMyPeopleScopeQuery, PeopleScope>
{
    public ValueTask<PeopleScope> Handle(GetMyPeopleScopeQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return scopeResolver.ResolveAsync(currentUser.GetUserId().ToString(), cancellationToken);
    }
}
