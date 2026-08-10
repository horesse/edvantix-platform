using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Subjects;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.ReorderSubjects;

public static class ReorderSubjectsEndpoint
{
    internal static RouteHandlerBuilder MapReorderSubjectsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/subjects/order",
                async ([FromBody] ReorderBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ReorderSubjectsCommand(body.ParentId, body.OrderedSubjectIds), ct);
                    return Results.NoContent();
                })
            .WithName("ReorderSubjects")
            .WithSummary("Set the sort order of subjects under a parent (or the top level)")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(CurriculumPermissions.Subjects.Update);

    public sealed record ReorderBody(Guid? ParentId, IReadOnlyList<Guid> OrderedSubjectIds);
}
