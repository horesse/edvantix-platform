using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.RemoveStudentGuardian;

public static class RemoveStudentGuardianEndpoint
{
    internal static RouteHandlerBuilder MapRemoveStudentGuardianEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/students/{studentId:guid}/guardians/{guardianId:guid}",
                async (Guid studentId, Guid guardianId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new RemoveStudentGuardianCommand(studentId, guardianId), ct);
                    return Results.NoContent();
                })
            .WithName("RemoveStudentGuardian")
            .WithSummary("Unlink a guardian from a student")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PeoplePermissions.Students.Update);
    }
}
