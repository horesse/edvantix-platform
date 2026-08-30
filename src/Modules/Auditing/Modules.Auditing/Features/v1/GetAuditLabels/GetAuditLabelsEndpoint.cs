using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Auditing.Contracts.Authorization;
using FSH.Modules.Auditing.Contracts.v1.GetAuditLabels;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Auditing.Features.v1.GetAuditLabels;

/// <summary>
/// Friendly labels for the entity type names and property names that appear in the audit log, so
/// the history UI shows "Ученик" / "Статус" instead of raw CLR names. Static reference data.
/// </summary>
public static class GetAuditLabelsEndpoint
{
    public static RouteHandlerBuilder MapGetAuditLabelsEndpoint(this IEndpointRouteBuilder group)
    {
        return group.MapGet(
                "/entity-labels",
                async (IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetAuditLabelsQuery(), cancellationToken)))
            .WithName("GetAuditLabels")
            .WithSummary("Friendly labels for audit entity and field names")
            .WithDescription("Maps the raw CLR type names and property names in the audit log to human-readable labels. An unlabelled name falls back to itself.")
            .RequirePermission(AuditingPermissions.AuditTrails.View)
            .Produces<AuditLabels>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
