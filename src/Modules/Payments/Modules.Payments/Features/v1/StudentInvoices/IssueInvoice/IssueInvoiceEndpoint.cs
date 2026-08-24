using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.IssueInvoice;

public static class IssueInvoiceEndpoint
{
    internal static RouteHandlerBuilder MapIssueInvoiceEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/student-invoices/{invoiceId:guid}/issue",
                async (Guid invoiceId, IssueInvoiceBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new IssueInvoiceCommand(invoiceId, body.IssuedOn), ct);
                    return Results.NoContent();
                })
            .WithName("IssueStudentInvoice")
            .WithSummary("Issue a draft invoice")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PaymentsPermissions.StudentInvoices.Issue);

    public sealed record IssueInvoiceBody(DateOnly IssuedOn);
}
