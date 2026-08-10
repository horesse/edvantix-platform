using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Teachers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Teachers.LinkTeacherUser;

public static class LinkTeacherUserEndpoint
{
    internal static RouteHandlerBuilder MapLinkTeacherUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/teachers/{teacherId:guid}/link-user",
                async (Guid teacherId, LinkUserRequest request, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(request);
                    await mediator.Send(new LinkTeacherUserCommand(teacherId, request.UserId), ct);
                    return Results.NoContent();
                })
            .WithName("LinkTeacherUser")
            .WithSummary("Link a teacher to an Identity user account")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(PeoplePermissions.Teachers.Update);
    }
}

public sealed record LinkUserRequest(string UserId);
