using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.BulkGenerateInvoices;

/// <summary>Named <c>GenerateInvoicesEndpoint</c>, not <c>BulkGenerateInvoicesEndpoint</c> — "Bulk" is
/// not a recognized leading verb under <c>Architecture.Tests</c>' endpoint-naming convention
/// (<c>EndpointConventionTests.Endpoint_Names_Should_Follow_Convention</c>); the command/route/folder
/// keep the "Bulk" prefix since only endpoint class names are checked.</summary>
public static class GenerateInvoicesEndpoint
{
    internal static RouteHandlerBuilder MapGenerateInvoicesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/student-invoices/bulk-generate",
                async (BulkGenerateInvoicesCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("BulkGenerateInvoices")
            .WithSummary("Generate draft invoices for a study group's active roster over a period")
            .RequirePermission(PaymentsPermissions.StudentInvoices.Create)
            .WithIdempotency();
}
