using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1.Teachers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Teachers.SearchTeachers;

public static class SearchTeachersEndpoint
{
    internal static RouteHandlerBuilder MapSearchTeachersEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/teachers",
                (string? search, TeacherStatus? status, int pageNumber, int pageSize,
                 string? sortBy, string? sortDir,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(
                        new SearchTeachersQuery(
                            search, status, pageNumber == 0 ? 1 : pageNumber, pageSize == 0 ? 50 : pageSize,
                            sortBy, sortDir),
                        ct))
            .WithName("SearchTeachers")
            .WithSummary("Search teachers (paged, filter by status, sortable)")
            .RequirePermission(PeoplePermissions.Teachers.View);
    }
}
