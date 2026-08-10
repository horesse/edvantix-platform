using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Subjects;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.DeleteSubject;

public static class DeleteSubjectEndpoint
{
    internal static RouteHandlerBuilder MapDeleteSubjectEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/subjects/{subjectId:guid}",
                async (Guid subjectId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteSubjectCommand(subjectId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteSubject")
            .WithSummary("Delete a curriculum subject")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .RequirePermission(CurriculumPermissions.Subjects.Delete);
}
