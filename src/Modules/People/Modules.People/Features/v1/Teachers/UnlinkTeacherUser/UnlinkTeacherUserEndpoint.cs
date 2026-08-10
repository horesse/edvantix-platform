using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Teachers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Teachers.UnlinkTeacherUser;

public static class UnlinkTeacherUserEndpoint
{
    internal static RouteHandlerBuilder MapUnlinkTeacherUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/teachers/{teacherId:guid}/unlink-user",
                async (Guid teacherId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new UnlinkTeacherUserCommand(teacherId), ct);
                    return Results.NoContent();
                })
            .WithName("UnlinkTeacherUser")
            .WithSummary("Unlink a teacher from its Identity user account")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(PeoplePermissions.Teachers.Update);
    }
}
