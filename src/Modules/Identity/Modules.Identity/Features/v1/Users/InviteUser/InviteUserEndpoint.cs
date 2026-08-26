using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Contracts.v1.Users.InviteUser;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.InviteUser;

public static class InviteUserEndpoint
{
    internal static RouteHandlerBuilder MapInviteUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/invite", async (InviteUserCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return TypedResults.Created($"/api/v1/identity/users/{result.UserId}", result);
        })
        .WithName("InviteUser")
        .WithSummary("Invite user")
        .WithDescription(
            "Create a new, unconfirmed user account for the given e-mail and school role, then " +
            "send a set-your-password link. The role must be one of the seeded school roles — " +
            "free-form roles are rejected.")
        .RequirePermission(IdentityPermissions.Users.Invite)
        .WithIdempotency()
        .Produces<InviteUserResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
