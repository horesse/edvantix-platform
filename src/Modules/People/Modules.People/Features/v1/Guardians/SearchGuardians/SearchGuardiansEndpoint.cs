using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Guardians;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Guardians.SearchGuardians;

public static class SearchGuardiansEndpoint
{
    internal static RouteHandlerBuilder MapSearchGuardiansEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/guardians",
                (string? search, int pageNumber, int pageSize, string? sortBy, string? sortDir,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(
                        new SearchGuardiansQuery(
                            search, pageNumber == 0 ? 1 : pageNumber, pageSize == 0 ? 50 : pageSize, sortBy, sortDir),
                        ct))
            .WithName("SearchGuardians")
            .WithSummary("Search guardians (paged, sortable)")
            .RequirePermission(PeoplePermissions.Guardians.View);
    }
}
