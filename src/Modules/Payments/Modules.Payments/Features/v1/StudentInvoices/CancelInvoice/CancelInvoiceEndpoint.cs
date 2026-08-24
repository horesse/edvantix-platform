using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.CancelInvoice;

public static class CancelInvoiceEndpoint
{
    internal static RouteHandlerBuilder MapCancelInvoiceEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/student-invoices/{invoiceId:guid}/cancel",
                async (Guid invoiceId, CancelInvoiceBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new CancelInvoiceCommand(invoiceId, body.Reason), ct);
                    return Results.NoContent();
                })
            .WithName("CancelInvoice")
            .WithSummary("Cancel an unpaid invoice")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PaymentsPermissions.StudentInvoices.Cancel);

    public sealed record CancelInvoiceBody(string? Reason);
}
