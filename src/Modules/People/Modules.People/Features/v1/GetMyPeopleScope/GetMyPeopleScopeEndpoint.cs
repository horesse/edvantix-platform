using FSH.Modules.People.Contracts.v1;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.GetMyPeopleScope;

public static class GetMyPeopleScopeEndpoint
{
    internal static RouteHandlerBuilder MapGetMyPeopleScopeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        // No RequirePermission — any authenticated user may ask "who am I in the domain"
        // (matches Multitenancy's GetMyTenantStatus, same self-lookup shape).
        return endpoints.MapGet("/people/me/scope",
                (IMediator mediator, CancellationToken ct) => mediator.Send(new GetMyPeopleScopeQuery(), ct))
            .WithName("GetMyPeopleScope")
            .WithSummary("Resolve the caller's own People scope (Student/Teacher/Guardian identity)");
    }
}
