using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.EnrollStudents;

public static class EnrollStudentsEndpoint
{
    internal static RouteHandlerBuilder MapEnrollStudentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/study-groups/{studyGroupId:guid}/enrollments",
                async (Guid studyGroupId, EnrollStudentsCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { StudyGroupId = studyGroupId };
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("EnrollStudents")
            .WithSummary("Enroll one or more students in a study group")
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .RequirePermission(StudyGroupsPermissions.Enrollments.Create)
            .WithIdempotency();
    }
}
