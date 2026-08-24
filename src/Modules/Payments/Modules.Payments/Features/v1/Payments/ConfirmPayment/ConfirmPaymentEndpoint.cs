using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.Payments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.Payments.ConfirmPayment;

public static class ConfirmPaymentEndpoint
{
    internal static RouteHandlerBuilder MapConfirmPaymentEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/student-invoices/{invoiceId:guid}/payments",
                async (Guid invoiceId, ConfirmPaymentBody body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ConfirmPaymentCommand(invoiceId, body.Amount, body.PaidOn, body.Method, body.Reference, body.ProofFileId, body.Note),
                        ct)))
            .WithName("ConfirmPayment")
            .WithSummary("Record a manager-confirmed payment against an invoice")
            .RequirePermission(PaymentsPermissions.StudentPayments.Confirm)
            .WithIdempotency();

    public sealed record ConfirmPaymentBody(decimal Amount, DateOnly PaidOn, PaymentMethod Method, string? Reference, Guid? ProofFileId, string? Note);
}
