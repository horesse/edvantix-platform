using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.v1.Tariffs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.Tariffs.DeactivateTariff;

public static class DeactivateTariffEndpoint
{
    internal static RouteHandlerBuilder MapDeactivateTariffEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/tariffs/{tariffId:guid}/deactivate",
                async (Guid tariffId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeactivateTariffCommand(tariffId), ct);
                    return Results.NoContent();
                })
            .WithName("DeactivateTariff")
            .WithSummary("Deactivate a tariff")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PaymentsPermissions.Tariffs.Manage);
}
