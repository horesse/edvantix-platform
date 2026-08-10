using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Guardians;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Guardians.UnlinkGuardianUser;

public static class UnlinkGuardianUserEndpoint
{
    internal static RouteHandlerBuilder MapUnlinkGuardianUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/guardians/{guardianId:guid}/unlink-user",
                async (Guid guardianId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new UnlinkGuardianUserCommand(guardianId), ct);
                    return Results.NoContent();
                })
            .WithName("UnlinkGuardianUser")
            .WithSummary("Unlink a guardian from its Identity user account")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(PeoplePermissions.Guardians.Update);
    }
}
