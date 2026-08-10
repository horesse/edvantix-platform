using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.SearchStudents;

public static class SearchStudentsEndpoint
{
    internal static RouteHandlerBuilder MapSearchStudentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/students",
                (string? search, StudentStatus? status, string? managerUserId, int pageNumber, int pageSize,
                 string? sortBy, string? sortDir,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(
                        new SearchStudentsQuery(
                            search,
                            status,
                            managerUserId,
                            pageNumber == 0 ? 1 : pageNumber,
                            pageSize == 0 ? 50 : pageSize,
                            sortBy,
                            sortDir),
                        ct))
            .WithName("SearchStudents")
            .WithSummary("Search students (paged, filter by status/manager, sortable)")
            .RequirePermission(PeoplePermissions.Students.View);
    }
}
