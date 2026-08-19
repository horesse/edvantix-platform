using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.SearchStudyGroups;

public static class SearchStudyGroupsEndpoint
{
    internal static RouteHandlerBuilder MapSearchStudyGroupsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/study-groups",
                async (
                    string? search,
                    Guid? courseId,
                    Guid? teacherId,
                    StudyGroupStatus? status,
                    GroupFormat? format,
                    int pageNumber,
                    int pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var query = new SearchStudyGroupsQuery(
                        search, courseId, teacherId, status, format,
                        pageNumber == 0 ? 1 : pageNumber,
                        pageSize == 0 ? 50 : pageSize,
                        sortBy, sortDir);
                    return Results.Ok(await mediator.Send(query, ct));
                })
            .WithName("SearchStudyGroups")
            .WithSummary("Search study groups")
            .RequirePermission(StudyGroupsPermissions.StudyGroups.View);
    }
}
