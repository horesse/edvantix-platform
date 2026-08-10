using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.UnlinkStudentUser;

public static class UnlinkStudentUserEndpoint
{
    internal static RouteHandlerBuilder MapUnlinkStudentUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/students/{studentId:guid}/unlink-user",
                async (Guid studentId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new UnlinkStudentUserCommand(studentId), ct);
                    return Results.NoContent();
                })
            .WithName("UnlinkStudentUser")
            .WithSummary("Unlink a student from its Identity user account")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(PeoplePermissions.Students.Update);
    }
}
