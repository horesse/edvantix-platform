using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.BulkIssueInvoices;

/// <summary>Named <c>IssueInvoicesEndpoint</c> — see <c>GenerateInvoicesEndpoint</c>'s remark on why
/// "Bulk" is dropped from the class name (verb-noun convention), kept on the route/command.</summary>
public static class IssueInvoicesEndpoint
{
    internal static RouteHandlerBuilder MapIssueInvoicesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/student-invoices/bulk-issue",
                async (BulkIssueInvoicesCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("BulkIssueInvoices")
            .WithSummary("Issue several draft invoices at once")
            .RequirePermission(PaymentsPermissions.StudentInvoices.Issue)
            .WithIdempotency();
}
