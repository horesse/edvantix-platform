using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.LinkStudentUser;

public static class LinkStudentUserEndpoint
{
    internal static RouteHandlerBuilder MapLinkStudentUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/students/{studentId:guid}/link-user",
                async (Guid studentId, LinkUserRequest request, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(request);
                    await mediator.Send(new LinkStudentUserCommand(studentId, request.UserId), ct);
                    return Results.NoContent();
                })
            .WithName("LinkStudentUser")
            .WithSummary("Link a student to an Identity user account")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(PeoplePermissions.Students.Update);
    }
}

public sealed record LinkUserRequest(string UserId);
