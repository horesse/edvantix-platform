using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.v1.Tariffs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.Tariffs.UpdateTariff;

public static class UpdateTariffEndpoint
{
    internal static RouteHandlerBuilder MapUpdateTariffEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/tariffs/{tariffId:guid}",
                async (Guid tariffId, UpdateTariffBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(
                        new UpdateTariffCommand(
                            tariffId, body.Name, body.CourseId, body.Amount, body.LessonsCount, body.ValidDays, body.ChargeOnExcusedAbsence),
                        ct);
                    return Results.NoContent();
                })
            .WithName("UpdateTariff")
            .WithSummary("Update a tariff")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PaymentsPermissions.Tariffs.Manage);

    public sealed record UpdateTariffBody(
        string Name, Guid? CourseId, decimal Amount, int LessonsCount, int ValidDays, bool ChargeOnExcusedAbsence);
}
