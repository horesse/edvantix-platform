using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Payments.Contracts.Authorization;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.CreateStudentInvoice;

public static class CreateStudentInvoiceEndpoint
{
    internal static RouteHandlerBuilder MapCreateStudentInvoiceEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/student-invoices",
                async (CreateStudentInvoiceCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateStudentInvoice")
            .WithSummary("Create a draft student invoice")
            .RequirePermission(PaymentsPermissions.StudentInvoices.Create)
            .WithIdempotency();
}
