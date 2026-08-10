using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Guardians;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Guardians.DeleteGuardian;

public static class DeleteGuardianEndpoint
{
    internal static RouteHandlerBuilder MapDeleteGuardianEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/guardians/{guardianId:guid}",
                async (Guid guardianId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteGuardianCommand(guardianId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteGuardian")
            .WithSummary("Delete a guardian")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PeoplePermissions.Guardians.Delete);
    }
}
