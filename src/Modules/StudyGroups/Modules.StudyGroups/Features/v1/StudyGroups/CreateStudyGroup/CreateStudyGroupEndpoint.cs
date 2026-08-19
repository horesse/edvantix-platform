using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.CreateStudyGroup;

public static class CreateStudyGroupEndpoint
{
    internal static RouteHandlerBuilder MapCreateStudyGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/study-groups",
                async (CreateStudyGroupCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateStudyGroup")
            .WithSummary("Create a study group")
            .RequirePermission(StudyGroupsPermissions.StudyGroups.Create)
            .WithIdempotency();
    }
}
