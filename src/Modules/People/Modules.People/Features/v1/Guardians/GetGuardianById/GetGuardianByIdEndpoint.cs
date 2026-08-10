using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Guardians;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Guardians.GetGuardianById;

public static class GetGuardianByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetGuardianByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/guardians/{guardianId:guid}",
                (Guid guardianId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetGuardianByIdQuery(guardianId), ct))
            .WithName("GetGuardianById")
            .WithSummary("Get a guardian by id")
            .RequirePermission(PeoplePermissions.Guardians.View);
    }
}
