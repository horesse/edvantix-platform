using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Guardians;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Guardians.CreateGuardian;

public static class CreateGuardianEndpoint
{
    internal static RouteHandlerBuilder MapCreateGuardianEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/guardians",
                async (CreateGuardianCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateGuardian")
            .WithSummary("Create a guardian")
            .RequirePermission(PeoplePermissions.Guardians.Create)
            .WithIdempotency();
    }
}
