using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Guardians;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Guardians.UpdateGuardian;

public static class UpdateGuardianEndpoint
{
    internal static RouteHandlerBuilder MapUpdateGuardianEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/guardians/{guardianId:guid}",
                async (Guid guardianId, UpdateGuardianCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { GuardianId = guardianId };
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("UpdateGuardian")
            .WithSummary("Update a guardian")
            .RequirePermission(PeoplePermissions.Guardians.Update);
    }
}
