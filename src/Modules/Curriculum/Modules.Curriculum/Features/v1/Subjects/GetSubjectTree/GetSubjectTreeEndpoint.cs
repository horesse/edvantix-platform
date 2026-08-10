using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Subjects;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.GetSubjectTree;

public static class GetSubjectTreeEndpoint
{
    internal static RouteHandlerBuilder MapGetSubjectTreeEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/subjects/tree",
                async (IMediator mediator, CancellationToken ct) =>
                    await mediator.Send(new GetSubjectTreeQuery(), ct))
            .WithName("GetSubjectTree")
            .WithSummary("Get the subject hierarchy as a tree")
            .RequirePermission(CurriculumPermissions.Subjects.View);
}
