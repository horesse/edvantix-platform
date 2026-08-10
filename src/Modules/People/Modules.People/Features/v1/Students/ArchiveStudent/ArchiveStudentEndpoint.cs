using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.ArchiveStudent;

public static class ArchiveStudentEndpoint
{
    internal static RouteHandlerBuilder MapArchiveStudentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/students/{studentId:guid}/archive",
                async (Guid studentId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ArchiveStudentCommand(studentId), ct);
                    return Results.NoContent();
                })
            .WithName("ArchiveStudent")
            .WithSummary("Archive a student (Active/Paused/Lead → Archived)")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(PeoplePermissions.Students.Update);
    }
}
