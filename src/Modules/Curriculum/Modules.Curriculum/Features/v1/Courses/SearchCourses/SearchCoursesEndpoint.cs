using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Courses.SearchCourses;

public static class SearchCoursesEndpoint
{
    internal static RouteHandlerBuilder MapSearchCoursesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/courses",
                (string? search, Guid? subjectId, CourseStatus? status, CourseLevel? level,
                 int pageNumber, int pageSize, string? sortBy, string? sortDir,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(
                        new SearchCoursesQuery(
                            search,
                            subjectId,
                            status,
                            level,
                            pageNumber == 0 ? 1 : pageNumber,
                            pageSize == 0 ? 20 : pageSize,
                            sortBy,
                            sortDir),
                        ct))
            .WithName("SearchCourses")
            .WithSummary("Search courses (paged, filter by subject/status/level, sortable)")
            .RequirePermission(CurriculumPermissions.Courses.View);
    }
}
