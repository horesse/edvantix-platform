using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetMyInvoices;

public static class GetMyInvoicesEndpoint
{
    internal static RouteHandlerBuilder MapGetMyInvoicesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/student-invoices/my",
                async (InvoiceStatus? status, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetMyInvoicesQuery(status), ct)))
            .WithName("GetMyStudentInvoices")
            .WithSummary("List the caller's own invoices, or their wards' invoices")
            .Produces<IReadOnlyList<StudentInvoiceDto>>()
            .RequirePermission(PaymentsPermissions.StudentInvoices.ViewOwn);
}
