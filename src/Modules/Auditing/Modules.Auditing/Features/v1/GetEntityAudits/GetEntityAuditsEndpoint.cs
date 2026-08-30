using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Auditing.Contracts.Authorization;
using FSH.Modules.Auditing.Contracts.Dtos;
using FSH.Modules.Auditing.Contracts.v1.GetAudits;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Auditing.Features.v1.GetEntityAudits;

/// <summary>
/// "История этого ученика/счёта/занятия" — the change history of one entity, for an entity
/// card. A thin shell over <see cref="GetAuditsQuery"/>: it fills <c>EntityName</c> and builds
/// the unified <c>EntityKey</c> (<c>Id:{entityId}</c>) so the caller passes just the raw id.
/// Composite-key entities go through <c>GET /audits</c> with an explicit <c>entityKey</c>.
/// </summary>
public static class GetEntityAuditsEndpoint
{
    public static RouteHandlerBuilder MapGetEntityAuditsEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet(
                "/by-entity/{entityName}/{entityId}",
                async (
                    string entityName,
                    string entityId,
                    int? pageNumber,
                    int? pageSize,
                    DateTime? fromUtc,
                    DateTime? toUtc,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetAuditsQuery
                    {
                        EntityName = entityName,
                        EntityKey = $"Id:{entityId}",
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        FromUtc = fromUtc,
                        ToUtc = toUtc,
                    }, cancellationToken)))
            .WithName("GetEntityAudits")
            .WithSummary("Get the change history of a single entity")
            .WithDescription("Audit events for one entity, matched on EntityName + Id:{entityId}. Paginated and date-windowed like GET /audits.")
            .RequirePermission(AuditingPermissions.AuditTrails.View)
            .Produces<PagedResponse<AuditSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
