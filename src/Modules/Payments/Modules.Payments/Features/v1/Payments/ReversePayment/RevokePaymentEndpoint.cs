using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.v1.Payments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.Payments.ReversePayment;

/// <summary>Named <c>RevokePaymentEndpoint</c>, not <c>ReversePaymentEndpoint</c> — "Reverse" is not a
/// recognized leading verb under <c>Architecture.Tests</c>' endpoint-naming convention, but "Revoke"
/// is (and matches <see cref="PaymentsPermissions.StudentPayments.Revoke"/>, the permission this
/// endpoint requires). The command/route keep "Reverse" — it's the domain vocabulary
/// (<c>StudentInvoice.ReversePayment</c>).</summary>
public static class RevokePaymentEndpoint
{
    internal static RouteHandlerBuilder MapRevokePaymentEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/payments/{paymentId:guid}/reverse",
                async (Guid paymentId, ReversePaymentBody body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new ReversePaymentCommand(paymentId, body.Note), ct)))
            .WithName("ReversePayment")
            .WithSummary("Reverse a confirmed payment")
            .RequirePermission(PaymentsPermissions.StudentPayments.Revoke);

    public sealed record ReversePaymentBody(string? Note);
}
