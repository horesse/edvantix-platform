using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetInvoicePdf;

public static class GetInvoicePdfEndpoint
{
    internal static RouteHandlerBuilder MapGetInvoicePdfEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/student-invoices/{invoiceId:guid}/pdf",
                async (Guid invoiceId, IMediator mediator, CancellationToken ct) =>
                {
                    var pdf = await mediator.Send(new GetInvoicePdfQuery(invoiceId), ct);
                    return Results.File(pdf, "application/pdf", $"{invoiceId}.pdf");
                })
            .WithName("GetStudentInvoicePdf")
            .WithSummary("Download an invoice as PDF")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PaymentsPermissions.StudentInvoices.View);
}
