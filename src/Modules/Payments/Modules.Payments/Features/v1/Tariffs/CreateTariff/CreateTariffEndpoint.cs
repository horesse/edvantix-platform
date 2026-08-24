using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.v1.Tariffs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.Tariffs.CreateTariff;

public static class CreateTariffEndpoint
{
    internal static RouteHandlerBuilder MapCreateTariffEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/tariffs",
                async (CreateTariffCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateTariff")
            .WithSummary("Create a tariff")
            .RequirePermission(PaymentsPermissions.Tariffs.Manage)
            .WithIdempotency();
}
