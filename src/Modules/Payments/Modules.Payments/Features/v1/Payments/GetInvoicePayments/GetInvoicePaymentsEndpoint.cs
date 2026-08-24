using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.Payments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.Payments.GetInvoicePayments;

public static class GetInvoicePaymentsEndpoint
{
    internal static RouteHandlerBuilder MapGetInvoicePaymentsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/student-invoices/{invoiceId:guid}/payments",
                async (Guid invoiceId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetInvoicePaymentsQuery(invoiceId), ct)))
            .WithName("GetInvoicePayments")
            .WithSummary("List payment confirmations for an invoice")
            .Produces<IReadOnlyList<PaymentConfirmationDto>>()
            .RequirePermission(PaymentsPermissions.StudentPayments.View);
}
