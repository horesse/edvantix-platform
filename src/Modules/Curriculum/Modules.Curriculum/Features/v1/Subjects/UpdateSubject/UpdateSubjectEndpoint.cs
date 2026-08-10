using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Subjects;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.UpdateSubject;

public static class UpdateSubjectEndpoint
{
    internal static RouteHandlerBuilder MapUpdateSubjectEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/subjects/{subjectId:guid}",
                async (Guid subjectId, UpdateSubjectBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new UpdateSubjectCommand(subjectId, body.Name, body.ParentId), ct);
                    return Results.NoContent();
                })
            .WithName("UpdateSubject")
            .WithSummary("Update a curriculum subject")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CurriculumPermissions.Subjects.Update);

    public sealed record UpdateSubjectBody(string Name, Guid? ParentId);
}
