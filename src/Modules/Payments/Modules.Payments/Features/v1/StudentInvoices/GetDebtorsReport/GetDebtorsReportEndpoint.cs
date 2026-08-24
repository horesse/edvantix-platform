using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetDebtorsReport;

public static class GetDebtorsReportEndpoint
{
    internal static RouteHandlerBuilder MapGetDebtorsReportEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/reports/debtors",
                async (Guid? studyGroupId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetDebtorsReportQuery(studyGroupId), ct)))
            .WithName("GetDebtorsReport")
            .WithSummary("List students with overdue invoices")
            .Produces<IReadOnlyList<DebtorDto>>()
            .RequirePermission(PaymentsPermissions.StudentInvoices.Export);
}
