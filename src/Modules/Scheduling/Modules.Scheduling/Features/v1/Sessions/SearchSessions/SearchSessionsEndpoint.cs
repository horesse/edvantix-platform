using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.SearchSessions;

public static class SearchSessionsEndpoint
{
    internal static RouteHandlerBuilder MapSearchSessionsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/sessions",
                async (
                    Guid? studyGroupId,
                    Guid? teacherId,
                    Guid? roomId,
                    DateTimeOffset? from,
                    DateTimeOffset? to,
                    SessionStatus? status,
                    int pageNumber,
                    int pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var query = new SearchSessionsQuery(
                        studyGroupId, teacherId, roomId, from, to, status,
                        pageNumber == 0 ? 1 : pageNumber,
                        pageSize == 0 ? 50 : pageSize,
                        sortBy, sortDir);
                    return Results.Ok(await mediator.Send(query, ct));
                })
            .WithName("SearchSessions")
            .WithSummary("Search sessions")
            .RequirePermission(SchedulingPermissions.Sessions.View);
}
