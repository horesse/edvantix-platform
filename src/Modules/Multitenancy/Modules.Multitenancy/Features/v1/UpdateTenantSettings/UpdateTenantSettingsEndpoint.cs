using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Multitenancy.Contracts.Authorization;
using FSH.Modules.Multitenancy.Contracts.v1.UpdateTenantSettings;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Multitenancy.Features.v1.UpdateTenantSettings;

public static class UpdateTenantSettingsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/settings", async (UpdateTenantSettingsCommand command, IMediator mediator, CancellationToken cancellationToken) =>
            {
                await mediator.Send(command, cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("UpdateTenantSettings")
            .WithSummary("Update current tenant settings")
            .WithDescription("Update the settings (time zone, currency) for the current tenant.")
            .RequirePermission(MultitenancyPermissions.SchoolSettings.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
