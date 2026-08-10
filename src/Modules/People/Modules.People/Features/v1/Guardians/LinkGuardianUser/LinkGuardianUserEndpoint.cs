using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Guardians;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Guardians.LinkGuardianUser;

public static class LinkGuardianUserEndpoint
{
    internal static RouteHandlerBuilder MapLinkGuardianUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/guardians/{guardianId:guid}/link-user",
                async (Guid guardianId, LinkUserRequest request, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(request);
                    await mediator.Send(new LinkGuardianUserCommand(guardianId, request.UserId), ct);
                    return Results.NoContent();
                })
            .WithName("LinkGuardianUser")
            .WithSummary("Link a guardian to an Identity user account")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(PeoplePermissions.Guardians.Update);
    }
}

public sealed record LinkUserRequest(string UserId);
