using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.Tariffs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.Tariffs.GetTariffs;

public static class GetTariffsEndpoint
{
    internal static RouteHandlerBuilder MapGetTariffsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/tariffs",
                async (bool? isActive, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetTariffsQuery(isActive), ct)))
            .WithName("GetTariffs")
            .WithSummary("List tariffs")
            .Produces<IReadOnlyList<TariffDto>>()
            .RequirePermission(PaymentsPermissions.Tariffs.View);
}
