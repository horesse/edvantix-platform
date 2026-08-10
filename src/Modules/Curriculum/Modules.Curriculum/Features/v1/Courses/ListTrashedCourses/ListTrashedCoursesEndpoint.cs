using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Courses.ListTrashedCourses;

public static class ListTrashedCoursesEndpoint
{
    internal static RouteHandlerBuilder MapListTrashedCoursesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/courses/trash",
                (int pageNumber, int pageSize, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(
                        new ListTrashedCoursesQuery(pageNumber == 0 ? 1 : pageNumber, pageSize == 0 ? 20 : pageSize),
                        ct))
            .WithName("ListTrashedCourses")
            .WithSummary("List courses in trash")
            .RequirePermission(CurriculumPermissions.Courses.ViewTrash);
    }
}
