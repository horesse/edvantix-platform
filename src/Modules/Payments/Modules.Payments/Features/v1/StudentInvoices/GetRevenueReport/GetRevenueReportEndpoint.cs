using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetRevenueReport;

public static class GetRevenueReportEndpoint
{
    internal static RouteHandlerBuilder MapGetRevenueReportEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/reports/revenue",
                async (DateOnly periodFrom, DateOnly periodTo, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetRevenueReportQuery(periodFrom, periodTo), ct)))
            .WithName("GetRevenueReport")
            .WithSummary("Revenue received over a period, by payment method")
            .Produces<RevenueReportDto>()
            .RequirePermission(PaymentsPermissions.StudentInvoices.Export);
}
